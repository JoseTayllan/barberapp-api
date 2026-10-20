using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Security.Cryptography;
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
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace BarberApp.IntegrationTests.Payments;

public sealed class MercadoPagoRecoveryTests
{
    private const string BasePath = "/api/integracoes/mercado-pago";
    private const string Purpose = "BarberApp.MercadoPago.Tokens.v1";
    private const string OldRefresh = "ONLY-FOR-TESTS-RECOVERY-OLD-REFRESH";
    private const string Secret = "ONLY-FOR-TESTS-RECOVERY-CLIENT-SECRET";
    private static readonly TimeSpan Guard = TimeSpan.FromSeconds(10);

    [Fact]
    public async Task Renovar_DeveRegistrarPendenciaDuravel_AntesDeEnviarRefreshAoProvedor()
    {
        using var environment = new Ambiente { PausarProvedor = true };
        using var factory = environment.CriarApi();
        using var client = CriarClient(factory);
        await environment.GravarAsync(factory);
        using var request = CriarRequest("renovacao");
        var pending = client.SendAsync(request);
        try
        {
            await environment.Entrou.Task.WaitAsync(Guard);
            // Outro host e outro coordenador, mesmo armazenamento: não basta uma flag singleton.
            using var observer = environment.CriarApi();
            using var observerClient = CriarClient(observer);
            await ValidarPendenteAsync(observerClient);
            using var retryRequest = CriarRequest("renovacao");
            using var retry = await observerClient.SendAsync(retryRequest).WaitAsync(Guard);
            await ValidarBloqueioAsync(retry);
            Assert.Equal(1, environment.Chamadas);
        }
        finally
        {
            environment.Liberar.TrySetResult(true);
            using var response = await pending.WaitAsync(Guard);
        }
    }

    [Fact]
    public async Task Renovar_DeveEvitarEnvioDoRefresh_QuandoNaoConseguirRegistrarPendencia()
    {
        using var environment = new Ambiente();
        using var factory = environment.CriarApi();
        using var client = CriarClient(factory);
        await environment.GravarAsync(factory);
        environment.FalharQualquerEscrita = true;
        using var request = CriarRequest("renovacao");
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Equal(0, environment.Chamadas);
        using var body = await LerJsonSeguroAsync(response);
        Assert.Equal("MercadoPagoPendenciaNaoRegistrada", body.RootElement.GetProperty("title").GetString());
        await ValidarStatusAsync(client, "Conectado");
        await environment.ValidarTokensAntigosAsync(factory);
    }

    [Theory]
    [InlineData("timeout")]
    [InlineData("server-error")]
    [InlineData("invalid-response")]
    [InlineData("save-failure")]
    [InlineData("protection-failure")]
    public async Task Renovar_DeveManterEstadoSeguroAposFalha_EBloquearReusoMesmoEmNovoHost(string mode)
    {
        using var environment = new Ambiente { Modo = mode };
        using (var factory = environment.CriarApi())
        {
            using var client = CriarClient(factory);
            await environment.GravarAsync(factory);
            using var request = CriarRequest("renovacao");
            using var response = await client.SendAsync(request);
            Assert.Equal(mode is "server-error" or "invalid-response"
                ? HttpStatusCode.BadGateway : HttpStatusCode.ServiceUnavailable, response.StatusCode);
            Assert.Equal(1, environment.Chamadas);
            await ValidarPendenteAsync(client);
            await environment.ValidarTokensAntigosAsync(factory);
        }
        environment.Modo = "success";
        using var restarted = environment.CriarApi();
        using var restartedClient = CriarClient(restarted);
        await ValidarPendenteAsync(restartedClient);
        using var nextRequest = CriarRequest("renovacao");
        using var next = await restartedClient.SendAsync(nextRequest);
        await ValidarBloqueioAsync(next);
        Assert.Equal(1, environment.Chamadas);
    }

    [Fact]
    public async Task Renovar_DeveLimparPendenciaSomenteComTokensNovosSalvos_QuandoConcluirComSucesso()
    {
        using var environment = new Ambiente();
        using (var factory = environment.CriarApi())
        {
            using var client = CriarClient(factory);
            await environment.GravarAsync(factory);
            using var request = CriarRequest("renovacao");
            using var response = await client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            await ValidarStatusAsync(client, "Conectado");
        }
        using var restarted = environment.CriarApi();
        using var restartedClient = CriarClient(restarted);
        await ValidarStatusAsync(restartedClient, "Conectado");
        using var nextRequest = CriarRequest("renovacao");
        using var next = await restartedClient.SendAsync(nextRequest);
        Assert.Equal(HttpStatusCode.OK, next.StatusCode);
        Assert.Equal(2, environment.Chamadas);
        Assert.Equal("ONLY-FOR-TESTS-RECOVERY-REFRESH-1", environment.UltimoRefresh);
    }

    [Theory]
    [InlineData("success")]
    [InlineData("denied")]
    [InlineData("save-failure")]
    [InlineData("protection-failure")]
    public async Task Callback_DeveLiberarConexaoPendente_SomenteAposNovaAutorizacaoPersistida(string recoveryMode)
    {
        using var environment = new Ambiente { Modo = "timeout" };
        using (var factory = environment.CriarApi())
        {
            using var client = CriarClient(factory);
            await environment.GravarAsync(factory);
            using var request = CriarRequest("renovacao");
            using var response = await client.SendAsync(request);
            Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        }
        environment.Modo = recoveryMode;
        using var restarted = environment.CriarApi();
        using var restartedClient = CriarClient(restarted);
        await ValidarPendenteAsync(restartedClient);
        var state = Iniciar(restarted);
        var query = recoveryMode == "denied" ? "error=access_denied" : "code=ONLY-FOR-TESTS-RECOVERY-CODE";
        using var recovery = await restartedClient.GetAsync($"{BasePath}/callback?state={state}&{query}");
        Assert.Equal(recoveryMode is "success" or "denied" ? HttpStatusCode.OK
            : HttpStatusCode.ServiceUnavailable, recovery.StatusCode);
        if (recoveryMode == "success")
        {
            await ValidarStatusAsync(restartedClient, "Conectado");
            using var nextRequest = CriarRequest("renovacao");
            using var next = await restartedClient.SendAsync(nextRequest);
            Assert.Equal(HttpStatusCode.OK, next.StatusCode);
            Assert.Equal(3, environment.Chamadas);
        }
        else
        {
            await ValidarPendenteAsync(restartedClient);
            var calls = environment.Chamadas;
            using var nextRequest = CriarRequest("renovacao");
            using var next = await restartedClient.SendAsync(nextRequest);
            await ValidarBloqueioAsync(next);
            Assert.Equal(calls, environment.Chamadas);
            await environment.ValidarTokensAntigosAsync(restarted);
        }
    }

    private static async Task ValidarPendenteAsync(HttpClient client) =>
        await ValidarStatusAsync(client, "ReautorizacaoNecessaria");

    private static async Task ValidarStatusAsync(HttpClient client, string status)
    {
        using var request = CriarRequest("status");
        using var response = await client.SendAsync(request).WaitAsync(Guard);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var body = await LerJsonSeguroAsync(response);
        Assert.Equal(status, body.RootElement.GetProperty("status").GetString());
        Assert.Equal(123456L, body.RootElement.GetProperty("contaId").GetInt64());
        Assert.True(body.RootElement.GetProperty("sandbox").GetBoolean());
        Assert.Equal(TimeSpan.Zero, body.RootElement.GetProperty("expiraEm").GetDateTimeOffset().Offset);
        Assert.Equal(new[] { "contaId", "expiraEm", "sandbox", "status" },
            body.RootElement.EnumerateObject().Select(property => property.Name).OrderBy(name => name).ToArray());
    }

    private static async Task ValidarBloqueioAsync(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        using var body = await LerJsonSeguroAsync(response);
        Assert.Equal("MercadoPagoReautorizacaoNecessaria", body.RootElement.GetProperty("title").GetString());
    }

    private static async Task<JsonDocument> LerJsonSeguroAsync(HttpResponseMessage response)
    {
        Assert.True(response.Headers.CacheControl?.NoStore);
        var content = await response.Content.ReadAsStringAsync();
        foreach (var secret in new[] { "ONLY-FOR-TESTS-", "access_token", "refresh_token", "TokensProtegidos" })
            Assert.DoesNotContain(secret, content);
        return JsonDocument.Parse(content);
    }

    private static string Iniciar(WebApplicationFactory<BarberApp.API.Controllers.ServicosController> factory)
    {
        using var scope = factory.Services.CreateScope();
        var result = scope.ServiceProvider.GetRequiredService<IConexaoMercadoPagoService>().Iniciar("recovery-admin");
        return QueryHelpers.ParseQuery(new Uri(result.UrlAutorizacao).Query)["state"].ToString();
    }

    private static HttpClient CriarClient(WebApplicationFactory<BarberApp.API.Controllers.ServicosController> factory) =>
        factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

    private static HttpRequestMessage CriarRequest(string operation)
    {
        var request = new HttpRequestMessage(operation == "status" ? HttpMethod.Get : HttpMethod.Post, $"{BasePath}/{operation}");
        request.Headers.Add("X-API-Key", BarberAppApiFactory.ApiKey);
        var jwt = new JwtSecurityToken(
            issuer: "BarberApp.IntegrationTests", audience: "BarberApp.IntegrationTests",
            claims: new[] { new Claim(ClaimTypes.Role, "Admin"), new Claim(ClaimTypes.NameIdentifier, "recovery-admin") },
            expires: DateTime.UtcNow.AddMinutes(5),
            signingCredentials: new SigningCredentials(new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes("integration-tests-secret-key-with-at-least-32-characters")), SecurityAlgorithms.HmacSha256));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", new JwtSecurityTokenHandler().WriteToken(jwt));
        return request;
    }

    private sealed class Ambiente : IDisposable
    {
        private readonly InMemoryDatabaseRoot _root = new();
        private readonly string _databaseName = $"MercadoPagoRecovery-{Guid.NewGuid()}";
        private readonly IDataProtectionProvider _protection = new EphemeralDataProtectionProvider();
        public TaskCompletionSource<bool> Entrou { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource<bool> Liberar { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public string Modo { get; set; } = "success";
        public bool PausarProvedor { get; set; }
        public bool FalharQualquerEscrita { get; set; }
        public int Chamadas;
        public string? UltimoRefresh { get; set; }
        private string? _original;

        public WebApplicationFactory<BarberApp.API.Controllers.ServicosController> CriarApi() =>
            new BarberAppApiFactory().WithWebHostBuilder(builder =>
            {
                builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(
                    new Dictionary<string, string?>
                    {
                        ["MercadoPago:ClientId"] = "123456789",
                        ["MercadoPago:ClientSecret"] = Secret,
                        ["MercadoPago:ConexaoEnabled"] = "true",
                        ["MercadoPago:Sandbox"] = "true",
                        ["MercadoPago:RedirectUri"] = "https://barberapp.example.test" + BasePath + "/callback"
                    }));
                builder.ConfigureTestServices(services =>
                {
                    services.RemoveAll<AppDbContext>();
                    services.RemoveAll<DbContextOptions<AppDbContext>>();
                    services.RemoveAll<IDbContextOptionsConfiguration<AppDbContext>>();
                    services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase(_databaseName, _root)
                        .AddInterceptors(new FalhaEscrita(this)));
                    services.RemoveAll<IDataProtectionProvider>();
                    services.AddSingleton<IDataProtectionProvider>(new Protecao(this));
                    services.RemoveAll<IHttpClientFactory>();
                    services.AddSingleton<IHttpClientFactory>(new Provedor(this));
                });
            });

        public async Task GravarAsync(WebApplicationFactory<BarberApp.API.Controllers.ServicosController> factory)
        {
            _original = _protection.CreateProtector(Purpose).Protect(JsonSerializer.Serialize(new
            {
                access_token = "ONLY-FOR-TESTS-RECOVERY-OLD-ACCESS",
                refresh_token = OldRefresh
            }));
            using var scope = factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            context.ConexoesMercadoPago.Add(new ConexaoMercadoPago(123456, "original-admin", _original,
                DateTimeOffset.UtcNow.AddHours(1), true));
            await context.SaveChangesAsync();
        }

        public async Task ValidarTokensAntigosAsync(WebApplicationFactory<BarberApp.API.Controllers.ServicosController> factory)
        {
            using var scope = factory.Services.CreateScope();
            var rows = await scope.ServiceProvider.GetRequiredService<AppDbContext>()
                .ConexoesMercadoPago.AsNoTracking().ToListAsync();
            var row = Assert.Single(rows);
            Assert.Equal(_original, row.TokensProtegidos);
            Assert.Equal("original-admin", row.AdministradorId);
        }

        public void Dispose() => Liberar.TrySetResult(true);

        private sealed class FalhaEscrita(Ambiente environment) : SaveChangesInterceptor
        {
            public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData,
                InterceptionResult<int> result, CancellationToken cancellationToken = default)
            {
                var entries = eventData.Context!.ChangeTracker.Entries<ConexaoMercadoPago>().ToArray();
                var anyChange = entries.Any(entry => entry.State == EntityState.Modified);
                var tokenChange = entries.Any(entry => entry.Property(row => row.TokensProtegidos).IsModified);
                if (environment.FalharQualquerEscrita && anyChange || environment.Modo == "save-failure" && tokenChange)
                    throw new DbUpdateException("ONLY-FOR-TESTS-RECOVERY-SAVE-FAILURE");
                return ValueTask.FromResult(result);
            }
        }

        private sealed class Protecao(Ambiente environment) : IDataProtectionProvider
        {
            public IDataProtector CreateProtector(string purpose) => new Protector(environment,
                environment._protection.CreateProtector(purpose));
            private sealed class Protector(Ambiente environment, IDataProtector inner) : IDataProtector
            {
                public IDataProtector CreateProtector(string purpose) => new Protector(environment, inner.CreateProtector(purpose));
                public byte[] Protect(byte[] plaintext) => environment.Modo == "protection-failure"
                    ? throw new CryptographicException("ONLY-FOR-TESTS-RECOVERY-PROTECTION-FAILURE") : inner.Protect(plaintext);
                public byte[] Unprotect(byte[] protectedData) => inner.Unprotect(protectedData);
            }
        }

        private sealed class Provedor(Ambiente environment) : HttpMessageHandler, IHttpClientFactory
        {
            public HttpClient CreateClient(string name) => new(this, disposeHandler: false);
            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                var call = Interlocked.Increment(ref environment.Chamadas);
                using var sent = JsonDocument.Parse(await request.Content!.ReadAsStringAsync(cancellationToken));
                if (sent.RootElement.TryGetProperty("refresh_token", out var refresh)) environment.UltimoRefresh = refresh.GetString();
                if (environment.PausarProvedor)
                {
                    environment.Entrou.TrySetResult(true);
                    await environment.Liberar.Task.WaitAsync(cancellationToken);
                }
                if (environment.Modo == "timeout") throw new TaskCanceledException("ONLY-FOR-TESTS-RECOVERY-TIMEOUT");
                if (environment.Modo == "server-error") return new HttpResponseMessage(HttpStatusCode.InternalServerError);
                var json = environment.Modo == "invalid-response" ? "{}" : JsonSerializer.Serialize(new
                {
                    access_token = $"ONLY-FOR-TESTS-RECOVERY-ACCESS-{call}",
                    refresh_token = $"ONLY-FOR-TESTS-RECOVERY-REFRESH-{call}",
                    token_type = "bearer",
                    expires_in = 3600,
                    user_id = 123456
                });
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                };
            }
        }
    }
}
