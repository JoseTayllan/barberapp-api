using System.Collections;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using BarberApp.Application.Services;
using BarberApp.Domain.Entities;
using BarberApp.Domain.Interfaces;
using BarberApp.Infrastructure.Data;
using BarberApp.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Npgsql;
using Xunit;

namespace BarberApp.IntegrationTests.Payments;

public sealed class MercadoPagoCredentialStorageTests
{
    private const string Endpoint = "/api/integracoes/mercado-pago/callback";
    private const string EntityName = "BarberApp.Domain.Entities.ConexaoMercadoPago";
    private const string Purpose = "BarberApp.MercadoPago.Tokens.v1";
    private const string AccessToken = "ONLY-FOR-TESTS-STORED-ACCESS-TOKEN";
    private const string RefreshToken = "ONLY-FOR-TESTS-STORED-REFRESH-TOKEN";

    [Fact]
    public async Task Callback_DevePersistirSomenteTokensProtegidos_AntesDeRetornarConectado()
    {
        using var factory = CriarFactory();
        using var client = CriarClient(factory);
        using var response = await client.GetAsync(Retorno(Iniciar(factory)));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var row = Assert.Single(LerConexoes(context));
        var entry = context.Entry(row);
        var protectedTokens = Assert.IsType<string>(entry.Property("TokensProtegidos").CurrentValue);
        Assert.DoesNotContain(AccessToken, protectedTokens);
        Assert.DoesNotContain(RefreshToken, protectedTokens);
        Assert.Equal(1, entry.Property("Id").CurrentValue);
        Assert.Equal(123456L, entry.Property("ContaId").CurrentValue);
        Assert.Equal("admin-storage-test", entry.Property("AdministradorId").CurrentValue);
        Assert.Equal(true, entry.Property("Sandbox").CurrentValue);
        var expiresAt = Assert.IsType<DateTimeOffset>(entry.Property("ExpiraEm").CurrentValue);
        Assert.InRange(expiresAt, DateTimeOffset.UtcNow.AddMinutes(59), DateTimeOffset.UtcNow.AddMinutes(61));
        var provider = scope.ServiceProvider.GetRequiredService<IDataProtectionProvider>();
        var plain = provider.CreateProtector(Purpose).Unprotect(protectedTokens);
        using var body = JsonDocument.Parse(plain);
        Assert.Equal(AccessToken, body.RootElement.GetProperty("access_token").GetString());
        Assert.Equal(RefreshToken, body.RootElement.GetProperty("refresh_token").GetString());
        Assert.Throws<CryptographicException>(() => provider.CreateProtector("wrong-purpose").Unprotect(protectedTokens));
        var content = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain(AccessToken, content);
        Assert.DoesNotContain(RefreshToken, content);
    }

    [Fact]
    public async Task Callback_DeveSubstituirConexaoDaInstalacao_SemCriarUmaContaPorAdmin()
    {
        using var factory = CriarFactory();
        using var client = CriarClient(factory);
        using var first = await client.GetAsync(Retorno(Iniciar(factory)));
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        using var second = await client.GetAsync(Retorno(Iniciar(factory, "other-admin")));
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var row = Assert.Single(LerConexoes(context));
        Assert.Equal("other-admin", context.Entry(row).Property("AdministradorId").CurrentValue);
    }

    [Fact]
    public async Task Callback_DeveFalharSeguramente_QuandoGravacaoFalhar()
    {
        using var factory = CriarFactory(falharGravacao: true);
        using var client = CriarClient(factory);
        var state = Iniciar(factory);
        using var response = await client.GetAsync(Retorno(state));
        await ValidarFalhaAsync(response, "MercadoPagoPersistenciaFalhou");
        using var scope = factory.Services.CreateScope();
        Assert.Empty(LerConexoes(scope.ServiceProvider.GetRequiredService<AppDbContext>()));
        using var replay = await client.GetAsync(Retorno(state));
        Assert.Equal(HttpStatusCode.BadRequest, replay.StatusCode);
    }

    [Fact]
    public async Task Callback_DeveEvitarPersistenciaPlaintext_QuandoProtecaoFalhar()
    {
        using var factory = CriarFactory(falharProtecao: true);
        using var client = CriarClient(factory);
        using var response = await client.GetAsync(Retorno(Iniciar(factory)));
        await ValidarFalhaAsync(response, "MercadoPagoProtecaoFalhou");
        using var scope = factory.Services.CreateScope();
        Assert.Empty(LerConexoes(scope.ServiceProvider.GetRequiredService<AppDbContext>()));
    }

    private static async Task ValidarFalhaAsync(HttpResponseMessage response, string title)
    {
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.True(response.Headers.CacheControl?.NoStore);
        var content = await response.Content.ReadAsStringAsync();
        using var body = JsonDocument.Parse(content);
        Assert.Equal(title, body.RootElement.GetProperty("title").GetString());
        Assert.DoesNotContain(AccessToken, content);
        Assert.DoesNotContain(RefreshToken, content);
        Assert.DoesNotContain("PRIVATE-FAILURE-ONLY-FOR-TESTS", content);
    }

    [Fact]
    public async Task Callback_DeveFalharSemDetalhes_QuandoBancoEstiverIndisponivel()
    {
        using var baseFactory = CriarFactory();
        using var factory = baseFactory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IConexaoMercadoPagoRepository>();
            services.AddScoped<IConexaoMercadoPagoRepository, BancoIndisponivel>();
        }));
        using var client = CriarClient(factory);
        using var response = await client.GetAsync(Retorno(Iniciar(factory)));
        await ValidarFalhaAsync(response, "MercadoPagoPersistenciaFalhou");
    }

    private sealed class BancoIndisponivel : IConexaoMercadoPagoRepository
    {
        public Task SalvarAsync(ConexaoMercadoPago conexao, CancellationToken cancellationToken) =>
            throw new NpgsqlException("PRIVATE-FAILURE-ONLY-FOR-TESTS");
    }

    // Consulta pelo modelo EF permite escrever a especificação antes de criar a entidade.
    private static object[] LerConexoes(AppDbContext context)
    {
        var entity = context.Model.FindEntityType(EntityName);
        Assert.NotNull(entity);
        var set = typeof(DbContext).GetMethod(nameof(DbContext.Set), Type.EmptyTypes)!
            .MakeGenericMethod(entity.ClrType).Invoke(context, null);
        return ((IEnumerable)set!).Cast<object>().ToArray();
    }

    private static string Retorno(string state) => $"{Endpoint}?state={state}&code=ONLY-FOR-TESTS-CODE";

    private static string Iniciar(WebApplicationFactory<BarberApp.API.Controllers.ServicosController> factory,
        string admin = "admin-storage-test")
    {
        using var scope = factory.Services.CreateScope();
        var response = scope.ServiceProvider.GetRequiredService<IConexaoMercadoPagoService>().Iniciar(admin);
        return QueryHelpers.ParseQuery(new Uri(response.UrlAutorizacao).Query)["state"].ToString();
    }

    private static HttpClient CriarClient(WebApplicationFactory<BarberApp.API.Controllers.ServicosController> factory) =>
        factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

    private static WebApplicationFactory<BarberApp.API.Controllers.ServicosController> CriarFactory(
        bool falharGravacao = false, bool falharProtecao = false) => new BarberAppApiFactory().WithWebHostBuilder(builder =>
    {
        var databaseName = $"MercadoPagoStorage-{Guid.NewGuid()}";
        var values = new Dictionary<string, string?>
        {
            ["MercadoPago:ClientId"] = "123456789",
            ["MercadoPago:ClientSecret"] = "ONLY-FOR-TESTS-CLIENT-SECRET",
            ["MercadoPago:RedirectUri"] = "https://barberapp.example.test" + Endpoint,
            ["MercadoPago:ConexaoEnabled"] = "true",
            ["MercadoPago:Sandbox"] = "true"
        };
        foreach (var setting in values) builder.UseSetting(setting.Key, setting.Value);
        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(values));
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<AppDbContext>();
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<AppDbContext>>();
            services.AddDbContext<AppDbContext>(options =>
            {
                options.UseInMemoryDatabase(databaseName);
                if (falharGravacao) options.AddInterceptors(new FalhaGravacao());
            });
            services.RemoveAll<IHttpClientFactory>();
            services.AddSingleton<IHttpClientFactory>(new ProvedorSimulado());
            if (falharProtecao)
            {
                services.RemoveAll<IDataProtectionProvider>();
                services.AddSingleton<IDataProtectionProvider>(new FalhaProtecao());
            }
        });
    });

    private sealed class ProvedorSimulado : HttpMessageHandler, IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(this, disposeHandler: false);
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(new
                {
                    access_token = AccessToken,
                    refresh_token = RefreshToken,
                    token_type = "bearer",
                    expires_in = 3600,
                    user_id = 123456
                }), Encoding.UTF8, "application/json")
            });
    }

    private sealed class FalhaGravacao : SaveChangesInterceptor
    {
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData,
            InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            if (eventData.Context!.ChangeTracker.Entries().Any(entry => entry.Metadata.Name == EntityName))
                throw new DbUpdateException("PRIVATE-FAILURE-ONLY-FOR-TESTS");
            return ValueTask.FromResult(result);
        }
    }

    private sealed class FalhaProtecao : IDataProtectionProvider, IDataProtector
    {
        public IDataProtector CreateProtector(string purpose) => this;
        public byte[] Protect(byte[] plaintext) => throw new CryptographicException("PRIVATE-FAILURE-ONLY-FOR-TESTS");
        public byte[] Unprotect(byte[] protectedData) => throw new CryptographicException();
    }
}
