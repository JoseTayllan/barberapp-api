using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using BarberApp.Application.Services;
using BarberApp.Domain.Entities;
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
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace BarberApp.IntegrationTests.Payments;

public sealed class MercadoPagoConcurrencyTests
{
    private const string BasePath = "/api/integracoes/mercado-pago";
    private const string Purpose = "BarberApp.MercadoPago.Tokens.v1";
    private const string Secret = "ONLY-FOR-TESTS-CONCURRENCY-CLIENT-SECRET";
    private const long OriginalAccount = 123456;
    private const long NewAccount = 654321;
    // Limite de diagnóstico para deadlock, não sincronização ou critério de desempenho.
    private static readonly TimeSpan Guard = TimeSpan.FromSeconds(10);

    [Theory]
    [InlineData("renovacao", "renovacao", false)]
    [InlineData("renovacao", "callback", false)]
    [InlineData("callback", "renovacao", false)]
    [InlineData("callback", "callback", false)]
    [InlineData("renovacao", "callback", true)]
    [InlineData("callback", "renovacao", true)]
    public async Task AlterarConexao_DeveRejeitarConcorrenteSemProvedor_EPermitirNovaTentativaAposConclusao(
        string firstOperation, string secondOperation, bool pauseAtSave)
    {
        using var scenario = new Cenario(pauseAtSave);
        await scenario.GravarAsync();
        var firstState = scenario.Iniciar("first-admin");
        var secondState = scenario.Iniciar("second-admin");
        using var firstRequest = CriarRequest(firstOperation, firstState);
        var firstTask = scenario.Client.SendAsync(firstRequest);
        HttpStatusCode? firstStatus = null;
        try
        {
            await scenario.Entrou.Task.WaitAsync(Guard);
            using var secondRequest = CriarRequest(secondOperation, secondState);
            using var second = await scenario.Client.SendAsync(secondRequest).WaitAsync(Guard);
            await ValidarConflitoAsync(second, firstState, secondState);
            Assert.Equal(1, scenario.Provider.Chamadas);
            Assert.Equal(scenario.Before, (await scenario.LerAsync()).TokensProtegidos);
        }
        finally
        {
            scenario.Liberar.TrySetResult(true);
            using var first = await firstTask.WaitAsync(Guard);
            firstStatus = first.StatusCode;
        }
        Assert.Equal(HttpStatusCode.OK, firstStatus);

        using var retryRequest = CriarRequest(secondOperation, secondState);
        using var retry = await scenario.Client.SendAsync(retryRequest).WaitAsync(Guard);
        Assert.Equal(HttpStatusCode.OK, retry.StatusCode);
        Assert.True(retry.Headers.CacheControl?.NoStore);
        Assert.Equal(2, scenario.Provider.Chamadas);
        var row = await scenario.LerAsync();
        var account = firstOperation == "callback" || secondOperation == "callback" ? NewAccount : OriginalAccount;
        Assert.Equal(account, row.ContaId);
        var originAdmin = secondOperation == "callback" ? "second-admin"
            : firstOperation == "callback" ? "first-admin" : "origin-admin";
        Assert.Equal(originAdmin, row.AdministradorId);
        using var tokens = JsonDocument.Parse(scenario.Protector.Unprotect(row.TokensProtegidos));
        Assert.Equal(Refresh(account, 2), tokens.RootElement.GetProperty("refresh_token").GetString());
        if (secondOperation == "renovacao")
            Assert.Equal(Refresh(account, 1), scenario.Provider.UltimoRefreshEnviado);
        else
        {
            using var replayRequest = CriarRequest("callback", secondState);
            using var replay = await scenario.Client.SendAsync(replayRequest).WaitAsync(Guard);
            Assert.Equal(HttpStatusCode.BadRequest, replay.StatusCode);
            Assert.Equal(2, scenario.Provider.Chamadas);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Renovar_DeveLiberarOperacaoAposFalha_SemBloquearNovasRequisicoes(bool saveFailure)
    {
        using var scenario = new Cenario(saveFailure);
        await scenario.GravarAsync();
        scenario.FalharGravacao = saveFailure;
        scenario.Provider.FalharPrimeira = !saveFailure;
        using var firstRequest = CriarRequest("renovacao");
        var firstTask = scenario.Client.SendAsync(firstRequest);
        HttpStatusCode? firstStatus = null;
        try
        {
            await scenario.Entrou.Task.WaitAsync(Guard);
            using var otherRequest = CriarRequest("renovacao");
            using var other = await scenario.Client.SendAsync(otherRequest).WaitAsync(Guard);
            await ValidarConflitoAsync(other);
            Assert.Equal(1, scenario.Provider.Chamadas);
        }
        finally
        {
            scenario.Liberar.TrySetResult(true);
            using var first = await firstTask.WaitAsync(Guard);
            firstStatus = first.StatusCode;
        }
        Assert.Equal(saveFailure ? HttpStatusCode.ServiceUnavailable : HttpStatusCode.BadGateway, firstStatus);
        Assert.Equal(scenario.Before, (await scenario.LerAsync()).TokensProtegidos);
        scenario.FalharGravacao = false;
        using var nextRequest = CriarRequest("renovacao");
        using var next = await scenario.Client.SendAsync(nextRequest).WaitAsync(Guard);
        Assert.Equal(HttpStatusCode.OK, next.StatusCode);
        Assert.Equal(2, scenario.Provider.Chamadas);
        // O provedor é simulado: esta recuperação não comprova validade remota após rotação sem gravação.
    }

    [Fact]
    public async Task ConsultarStatusEValidarCallback_DeveContinuarDisponivel_DuranteOperacao()
    {
        using var scenario = new Cenario();
        await scenario.GravarAsync();
        using var request = CriarRequest("renovacao");
        var firstTask = scenario.Client.SendAsync(request);
        try
        {
            await scenario.Entrou.Task.WaitAsync(Guard);
            using var statusRequest = CriarRequest("status");
            using var status = await scenario.Client.SendAsync(statusRequest).WaitAsync(Guard);
            Assert.Equal(HttpStatusCode.OK, status.StatusCode);
            using var body = JsonDocument.Parse(await status.Content.ReadAsStringAsync());
            Assert.Equal(OriginalAccount, body.RootElement.GetProperty("contaId").GetInt64());
            using var invalid = await scenario.Client.GetAsync($"{BasePath}/callback?state={new string('x', 43)}&code=ONLY-FOR-TESTS-CODE")
                .WaitAsync(Guard);
            Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
            Assert.Equal(1, scenario.Provider.Chamadas);
        }
        finally
        {
            scenario.Liberar.TrySetResult(true);
            using var first = await firstTask.WaitAsync(Guard);
        }
    }

    private static async Task ValidarConflitoAsync(HttpResponseMessage response, params string[] states)
    {
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.True(response.Headers.CacheControl?.NoStore);
        var content = await response.Content.ReadAsStringAsync();
        using var body = JsonDocument.Parse(content);
        Assert.Equal("MercadoPagoOperacaoEmAndamento", body.RootElement.GetProperty("title").GetString());
        Assert.DoesNotContain(Secret, content);
        Assert.DoesNotContain("ONLY-FOR-TESTS-", content);
        foreach (var state in states) Assert.DoesNotContain(state, content);
    }

    private static string Refresh(long account, int sequence) => $"ONLY-FOR-TESTS-REFRESH-{account}-{sequence}";

    private static HttpRequestMessage CriarRequest(string operation, string? state = null)
    {
        if (operation == "callback") return new HttpRequestMessage(HttpMethod.Get,
            $"{BasePath}/callback?state={state}&code=ONLY-FOR-TESTS-CODE");
        var request = new HttpRequestMessage(operation == "status" ? HttpMethod.Get : HttpMethod.Post, $"{BasePath}/{operation}");
        request.Headers.Add("X-API-Key", BarberAppApiFactory.ApiKey);
        var jwt = new JwtSecurityToken(
            issuer: "BarberApp.IntegrationTests", audience: "BarberApp.IntegrationTests",
            claims: new[] { new Claim(ClaimTypes.Role, "Admin"), new Claim(ClaimTypes.NameIdentifier, "requesting-admin") },
            expires: DateTime.UtcNow.AddMinutes(5),
            signingCredentials: new SigningCredentials(new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes("integration-tests-secret-key-with-at-least-32-characters")), SecurityAlgorithms.HmacSha256));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", new JwtSecurityTokenHandler().WriteToken(jwt));
        return request;
    }

    private sealed class Cenario : IDisposable
    {
        public TaskCompletionSource<bool> Entrou { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource<bool> Liberar { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public IDataProtector Protector { get; }
        public ProvedorControlado Provider { get; }
        public WebApplicationFactory<BarberApp.API.Controllers.ServicosController> Factory { get; }
        public HttpClient Client { get; }
        public string? Before { get; private set; }
        public bool FalharGravacao { get; set; }
        public bool GravarComPausa { get; private set; }
        private int _saves;

        public Cenario(bool pauseAtSave = false)
        {
            var protection = new EphemeralDataProtectionProvider();
            Protector = protection.CreateProtector(Purpose);
            Provider = new ProvedorControlado(this, pauseAtSave);
            var databaseName = $"MercadoPagoConcurrency-{Guid.NewGuid()}";
            Factory = new BarberAppApiFactory().WithWebHostBuilder(builder =>
            {
                builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(
                    new Dictionary<string, string?>
                    {
                        ["MercadoPago:ClientId"] = "123456789",
                        ["MercadoPago:ClientSecret"] = Secret,
                        ["MercadoPago:RedirectUri"] = "https://barberapp.example.test" + BasePath + "/callback",
                        ["MercadoPago:ConexaoEnabled"] = "true",
                        ["MercadoPago:Sandbox"] = "true"
                    }));
                builder.ConfigureTestServices(services =>
                {
                    services.RemoveAll<AppDbContext>();
                    services.RemoveAll<DbContextOptions<AppDbContext>>();
                    services.RemoveAll<IDbContextOptionsConfiguration<AppDbContext>>();
                    services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase(databaseName)
                        .AddInterceptors(new PausaGravacao(this, pauseAtSave)));
                    services.RemoveAll<IDataProtectionProvider>();
                    services.AddSingleton<IDataProtectionProvider>(protection);
                    services.RemoveAll<IHttpClientFactory>();
                    services.AddSingleton<IHttpClientFactory>(Provider);
                });
            });
            Client = Factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        }

        public async Task GravarAsync()
        {
            Before = Protector.Protect(JsonSerializer.Serialize(new
            {
                access_token = "ONLY-FOR-TESTS-OLD-ACCESS",
                refresh_token = Refresh(OriginalAccount, 0)
            }));
            using var scope = Factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            context.ConexoesMercadoPago.Add(new ConexaoMercadoPago(OriginalAccount, "origin-admin", Before,
                DateTimeOffset.UtcNow.AddHours(1), true));
            await context.SaveChangesAsync();
            GravarComPausa = true;
        }

        public string Iniciar(string admin)
        {
            using var scope = Factory.Services.CreateScope();
            var response = scope.ServiceProvider.GetRequiredService<IConexaoMercadoPagoService>().Iniciar(admin);
            return QueryHelpers.ParseQuery(new Uri(response.UrlAutorizacao).Query)["state"].ToString();
        }

        public async Task<ConexaoMercadoPago> LerAsync()
        {
            using var scope = Factory.Services.CreateScope();
            return Assert.Single(await scope.ServiceProvider.GetRequiredService<AppDbContext>()
                .ConexoesMercadoPago.AsNoTracking().ToListAsync());
        }

        private sealed class PausaGravacao(Cenario scenario, bool pauseAtSave) : SaveChangesInterceptor
        {
            public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData,
                InterceptionResult<int> result, CancellationToken cancellationToken = default)
            {
                if (!scenario.GravarComPausa) return result;
                if (pauseAtSave && Interlocked.Increment(ref scenario._saves) == 1)
                {
                    scenario.Entrou.TrySetResult(true);
                    await scenario.Liberar.Task.WaitAsync(cancellationToken);
                }
                if (scenario.FalharGravacao) throw new DbUpdateException("ONLY-FOR-TESTS-SAVE-FAILURE");
                return result;
            }
        }

        public void Dispose()
        {
            Liberar.TrySetResult(true);
            Client.Dispose();
            Factory.Dispose();
            Provider.Dispose();
        }
    }

    private sealed class ProvedorControlado(Cenario scenario, bool pauseAtSave) : HttpMessageHandler, IHttpClientFactory
    {
        private int _chamadas;
        public int Chamadas => Volatile.Read(ref _chamadas);
        public bool FalharPrimeira { get; set; }
        public string? UltimoRefreshEnviado { get; private set; }
        public HttpClient CreateClient(string name) => new(this, disposeHandler: false);

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var sequence = Interlocked.Increment(ref _chamadas);
            using var body = JsonDocument.Parse(await request.Content!.ReadAsStringAsync(cancellationToken));
            var grant = body.RootElement.GetProperty("grant_type").GetString();
            long account = NewAccount;
            if (grant == "refresh_token")
            {
                UltimoRefreshEnviado = body.RootElement.GetProperty("refresh_token").GetString();
                account = long.Parse(UltimoRefreshEnviado!.Split('-')[^2], System.Globalization.CultureInfo.InvariantCulture);
            }
            if (!pauseAtSave && sequence == 1)
            {
                scenario.Entrou.TrySetResult(true);
                await scenario.Liberar.Task.WaitAsync(cancellationToken);
            }
            if (FalharPrimeira && sequence == 1) return new HttpResponseMessage(HttpStatusCode.InternalServerError);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(new
                {
                    access_token = $"ONLY-FOR-TESTS-ACCESS-{account}-{sequence}",
                    refresh_token = Refresh(account, sequence),
                    token_type = "bearer",
                    expires_in = 3600,
                    user_id = account
                }), Encoding.UTF8, "application/json")
            };
        }
    }
}
