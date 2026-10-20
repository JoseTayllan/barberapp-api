using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using BarberApp.Domain.Entities;
using BarberApp.Infrastructure.Data;
using BarberApp.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace BarberApp.IntegrationTests.Payments;

public sealed class MercadoPagoTokenRenewalTests
{
    private const string Endpoint = "/api/integracoes/mercado-pago/renovacao";
    private const string Purpose = "BarberApp.MercadoPago.Tokens.v1";
    private const string Secret = "ONLY-FOR-TESTS-RENEWAL-CLIENT-SECRET";
    private const string OldAccess = "ONLY-FOR-TESTS-OLD-ACCESS";
    private const string OldRefresh = "ONLY-FOR-TESTS-OLD-REFRESH";
    private const string NewAccess = "ONLY-FOR-TESTS-NEW-ACCESS";
    private const string NewRefresh = "ONLY-FOR-TESTS-NEW-REFRESH";
    private static readonly DateTimeOffset Now = new(2026, 10, 19, 15, 0, 0, TimeSpan.Zero);
    private const string ValidResponse = """
        {"access_token":"ONLY-FOR-TESTS-NEW-ACCESS","refresh_token":"ONLY-FOR-TESTS-NEW-REFRESH",
         "token_type":"bearer","expires_in":3600,"user_id":123456,"scope":"offline_access"}
        """;

    [Theory]
    [InlineData("missing-key", 401)]
    [InlineData("invalid-key", 403)]
    [InlineData("missing-jwt", 401)]
    [InlineData("Cliente", 403)]
    [InlineData("Barbeiro", 403)]
    [InlineData("missing-id", 401)]
    public async Task Renovar_DeveExigirApiKeyEAdminIdentificado_SemContatarProvedor(string scenario, int status)
    {
        using var fixture = new Cenario();
        using var request = CriarRequest(scenario);
        using var response = await fixture.Client.SendAsync(request);
        Assert.Equal((HttpStatusCode)status, response.StatusCode);
        Assert.Equal(0, fixture.Provider.Chamadas);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(1)]
    public async Task Renovar_DeveRotacionarTokensProtegidos_EPreservarContaEAdminDeOrigem(int hoursToExpiry)
    {
        using var fixture = new Cenario();
        await fixture.GravarAsync(hoursToExpiry: hoursToExpiry);
        using var request = CriarRequest();
        using var response = await fixture.Client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.CacheControl?.NoStore);
        var content = await response.Content.ReadAsStringAsync();
        ValidarSemSegredos(content, fixture.Before!);
        using var body = JsonDocument.Parse(content);
        Assert.Equal(new[] { "contaId", "expiraEm", "sandbox", "status" },
            body.RootElement.EnumerateObject().Select(property => property.Name).OrderBy(name => name).ToArray());
        Assert.Equal("Conectado", body.RootElement.GetProperty("status").GetString());
        Assert.Equal(123456L, body.RootElement.GetProperty("contaId").GetInt64());
        Assert.Equal(Now.AddHours(1), body.RootElement.GetProperty("expiraEm").GetDateTimeOffset());
        Assert.True(body.RootElement.GetProperty("sandbox").GetBoolean());
        var row = await fixture.LerAsync();
        Assert.Equal(1, row.Id);
        Assert.Equal(123456L, row.ContaId);
        Assert.Equal("origin-admin-renewal-test", row.AdministradorId);
        Assert.True(row.Sandbox);
        Assert.Equal(Now.AddHours(1), row.ExpiraEm);
        Assert.NotEqual(fixture.Before, row.TokensProtegidos);
        Assert.DoesNotContain(NewAccess, row.TokensProtegidos);
        Assert.DoesNotContain(NewRefresh, row.TokensProtegidos);
        using var tokens = JsonDocument.Parse(fixture.Protector.Unprotect(row.TokensProtegidos));
        Assert.Equal(NewAccess, tokens.RootElement.GetProperty("access_token").GetString());
        Assert.Equal(NewRefresh, tokens.RootElement.GetProperty("refresh_token").GetString());
        Assert.Throws<CryptographicException>(() => fixture.Protection.CreateProtector("wrong-purpose")
            .Unprotect(row.TokensProtegidos));
        Assert.Equal(1, fixture.Provider.Chamadas);
        Assert.Equal(HttpMethod.Post, fixture.Provider.Method);
        Assert.Equal("https://api.mercadopago.com/oauth/token", fixture.Provider.Url);
        Assert.Equal("application/json", fixture.Provider.ContentType);
        using var sent = JsonDocument.Parse(fixture.Provider.Body!);
        Assert.Equal("refresh_token", sent.RootElement.GetProperty("grant_type").GetString());
        Assert.Equal(OldRefresh, sent.RootElement.GetProperty("refresh_token").GetString());
        Assert.Equal("123456789", sent.RootElement.GetProperty("client_id").GetString());
        Assert.Equal(Secret, sent.RootElement.GetProperty("client_secret").GetString());
        Assert.True(sent.RootElement.GetProperty("test_token").GetBoolean());
        Assert.False(sent.RootElement.TryGetProperty("code", out _));
        Assert.False(sent.RootElement.TryGetProperty("code_verifier", out _));
        Assert.DoesNotContain(OldAccess, fixture.Provider.Body!);
    }

    [Fact]
    public async Task Renovar_DeveRetornar409_QuandoNaoHouverConexao()
    {
        using var fixture = new Cenario();
        using var request = CriarRequest();
        using var response = await fixture.Client.SendAsync(request);
        await ValidarErroAsync(response, 409, "MercadoPagoNaoConectado");
        Assert.Equal(0, fixture.Provider.Chamadas);
    }

    [Theory]
    [InlineData("MercadoPago:ConexaoEnabled", "false", "MercadoPagoConexaoDesabilitada")]
    [InlineData("MercadoPago:ClientId", null, "MercadoPagoNaoConfigurado")]
    [InlineData("MercadoPago:ClientSecret", null, "MercadoPagoNaoConfigurado")]
    [InlineData("MercadoPago:Sandbox", "false", "MercadoPagoAmbienteIncompativel")]
    public async Task Renovar_DeveImpedirChamadaExterna_QuandoConfiguracaoForIncompativel(
        string key, string? value, string title)
    {
        using var fixture = new Cenario(key, value);
        await fixture.GravarAsync();
        using var request = CriarRequest();
        using var response = await fixture.Client.SendAsync(request);
        await ValidarErroAsync(response, 503, title, fixture.Before!);
        Assert.Equal(0, fixture.Provider.Chamadas);
        await fixture.ValidarPreservacaoAsync();
    }

    [Theory]
    [InlineData("corrupted")]
    [InlineData("malformed-json")]
    [InlineData("missing-refresh")]
    public async Task Renovar_DeveFalharSeguramente_QuandoCredenciaisLocaisForemInvalidas(string scenario)
    {
        using var fixture = new Cenario();
        await fixture.GravarAsync(payload: scenario);
        using var request = CriarRequest();
        using var response = await fixture.Client.SendAsync(request);
        await ValidarErroAsync(response, 503, "MercadoPagoCredenciaisIndisponiveis", fixture.Before!);
        Assert.Equal(0, fixture.Provider.Chamadas);
        await fixture.ValidarPreservacaoAsync();
    }

    [Theory]
    [InlineData(401)]
    [InlineData(429)]
    [InlineData(500)]
    public async Task Renovar_DevePreservarConexaoSemRetry_QuandoProvedorFalhar(int status)
    {
        using var fixture = new Cenario();
        fixture.Provider.Status = (HttpStatusCode)status;
        fixture.Provider.Response = "PRIVATE-FAILURE-ONLY-FOR-TESTS";
        await ValidarFalhaRemotaAsync(fixture, 502, "MercadoPagoProvedorFalhou");
    }

    [Fact]
    public async Task Renovar_DeveSolicitarNovaAutorizacao_QuandoRefreshTokenForRecusado()
    {
        using var fixture = new Cenario();
        fixture.Provider.Status = HttpStatusCode.BadRequest;
        fixture.Provider.Response = """{"error":"invalid_grant","message":"PRIVATE-FAILURE-ONLY-FOR-TESTS"}""";
        await ValidarFalhaRemotaAsync(fixture, 409, "MercadoPagoReautorizacaoNecessaria");
    }

    [Theory]
    [InlineData("not-json")]
    [InlineData("{}")]
    [InlineData("{\"access_token\":\"fake\",\"token_type\":\"bearer\",\"expires_in\":3600,\"user_id\":123456}")]
    [InlineData("{\"access_token\":\"fake\",\"refresh_token\":\"fake\",\"token_type\":\"bearer\",\"expires_in\":0,\"user_id\":123456}")]
    [InlineData("{\"access_token\":\"fake\",\"refresh_token\":\"fake\",\"token_type\":\"bearer\",\"expires_in\":3600,\"user_id\":999999}")]
    public async Task Renovar_DeveRejeitarRespostaInvalidaOuOutraConta_SemSubstituirConexao(string response)
    {
        using var fixture = new Cenario();
        fixture.Provider.Response = response;
        await ValidarFalhaRemotaAsync(fixture, 502, "MercadoPagoRespostaInvalida");
    }

    [Fact]
    public async Task Renovar_DeveRetornar503SemRetry_QuandoProvedorExcederTempo()
    {
        using var fixture = new Cenario();
        fixture.Provider.Timeout = true;
        await ValidarFalhaRemotaAsync(fixture, 503, "MercadoPagoIndisponivel");
    }

    [Theory]
    [InlineData(false, "MercadoPagoProtecaoFalhou")]
    [InlineData(true, "MercadoPagoPersistenciaFalhou")]
    public async Task Renovar_DevePreservarRegistroSemDeclararSucesso_QuandoProtecaoOuGravacaoFalhar(
        bool saveFailure, string title)
    {
        using var fixture = new Cenario();
        await fixture.GravarAsync();
        fixture.FalharGravacao = saveFailure;
        fixture.FalharProtecao = !saveFailure;
        using var request = CriarRequest();
        using var response = await fixture.Client.SendAsync(request);
        await ValidarErroAsync(response, 503, title, fixture.Before!);
        Assert.Equal(1, fixture.Provider.Chamadas);
        await fixture.ValidarPreservacaoAsync();
    }

    private static async Task ValidarFalhaRemotaAsync(Cenario fixture, int status, string title)
    {
        await fixture.GravarAsync();
        using var request = CriarRequest();
        using var response = await fixture.Client.SendAsync(request);
        await ValidarErroAsync(response, status, title, fixture.Before!);
        Assert.Equal(1, fixture.Provider.Chamadas);
        await fixture.ValidarPreservacaoAsync();
    }

    private static async Task ValidarErroAsync(HttpResponseMessage response, int status, string title, string? encrypted = null)
    {
        Assert.Equal((HttpStatusCode)status, response.StatusCode);
        Assert.True(response.Headers.CacheControl?.NoStore);
        var content = await response.Content.ReadAsStringAsync();
        ValidarSemSegredos(content, encrypted);
        using var body = JsonDocument.Parse(content);
        Assert.Equal(title, body.RootElement.GetProperty("title").GetString());
    }

    private static void ValidarSemSegredos(string content, string? encrypted)
    {
        foreach (var secret in new[] { Secret, OldAccess, OldRefresh, NewAccess, NewRefresh,
            "PRIVATE-FAILURE-ONLY-FOR-TESTS", "origin-admin-renewal-test", "access_token", "refresh_token" })
            Assert.DoesNotContain(secret, content);
        if (encrypted is not null) Assert.DoesNotContain(encrypted, content);
    }

    private static HttpRequestMessage CriarRequest(string scenario = "Admin")
    {
        var request = new HttpRequestMessage(HttpMethod.Post, Endpoint);
        if (scenario != "missing-key") request.Headers.Add("X-API-Key",
            scenario == "invalid-key" ? "ONLY-FOR-TESTS-INVALID-KEY" : BarberAppApiFactory.ApiKey);
        if (scenario == "missing-jwt") return request;
        var role = scenario is "Cliente" or "Barbeiro" ? scenario : "Admin";
        var claims = new List<Claim> { new(ClaimTypes.Role, role) };
        if (scenario != "missing-id") claims.Add(new Claim(ClaimTypes.NameIdentifier, "requesting-admin-renewal-test"));
        var jwt = new JwtSecurityToken(
            issuer: "BarberApp.IntegrationTests", audience: "BarberApp.IntegrationTests", claims: claims,
            expires: DateTime.UtcNow.AddMinutes(5),
            signingCredentials: new SigningCredentials(new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes("integration-tests-secret-key-with-at-least-32-characters")),
                SecurityAlgorithms.HmacSha256));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", new JwtSecurityTokenHandler().WriteToken(jwt));
        return request;
    }

    // Isolamento por cenário; nada é enviado a um provedor real ou ao banco da aplicação.
    private sealed class Cenario : IDisposable
    {
        public ProvedorSimulado Provider { get; } = new();
        public IDataProtectionProvider Protection { get; } = new EphemeralDataProtectionProvider();
        public IDataProtector Protector => Protection.CreateProtector(Purpose);
        public WebApplicationFactory<BarberApp.API.Controllers.ServicosController> Factory { get; }
        public HttpClient Client { get; }
        public string? Before { get; private set; }
        public bool FalharGravacao { get; set; }
        public bool FalharProtecao { get; set; }
        private DateTimeOffset _previousExpiry;

        public Cenario(string? key = null, string? value = null)
        {
            var databaseName = $"MercadoPagoRenewal-{Guid.NewGuid()}";
            Factory = new BarberAppApiFactory().WithWebHostBuilder(builder =>
            {
                var settings = new Dictionary<string, string?>
                {
                    ["MercadoPago:ClientId"] = "123456789",
                    ["MercadoPago:ClientSecret"] = Secret,
                    ["MercadoPago:ConexaoEnabled"] = "true",
                    ["MercadoPago:Sandbox"] = "true"
                };
                if (key is not null) settings[key] = value;
                builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(settings));
                builder.ConfigureTestServices(services =>
                {
                    services.RemoveAll<AppDbContext>();
                    services.RemoveAll<DbContextOptions<AppDbContext>>();
                    services.RemoveAll<IDbContextOptionsConfiguration<AppDbContext>>();
                    services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase(databaseName)
                        .AddInterceptors(new FalhaGravacao(this)));
                    services.RemoveAll<IHttpClientFactory>();
                    services.AddSingleton<IHttpClientFactory>(Provider);
                    services.RemoveAll<IDataProtectionProvider>();
                    services.AddSingleton<IDataProtectionProvider>(new ProtecaoControlada(this));
                    services.RemoveAll<TimeProvider>();
                    services.AddSingleton<TimeProvider>(new RelogioFixo());
                });
            });
            Client = Factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        }

        public async Task GravarAsync(int hoursToExpiry = -1, string? payload = null)
        {
            var plain = payload switch
            {
                "malformed-json" => "not-json",
                "missing-refresh" => """{"access_token":"ONLY-FOR-TESTS-OLD-ACCESS"}""",
                _ => JsonSerializer.Serialize(new
                {
                    access_token = OldAccess,
                    refresh_token = OldRefresh
                })
            };
            Before = payload == "corrupted" ? "ONLY-FOR-TESTS-CORRUPTED-CIPHERTEXT" : Protector.Protect(plain);
            _previousExpiry = Now.AddHours(hoursToExpiry);
            using var scope = Factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            context.ConexoesMercadoPago.Add(new ConexaoMercadoPago(123456, "origin-admin-renewal-test",
                Before, _previousExpiry, true));
            await context.SaveChangesAsync();
        }

        public async Task<ConexaoMercadoPago> LerAsync()
        {
            using var scope = Factory.Services.CreateScope();
            return Assert.Single(await scope.ServiceProvider.GetRequiredService<AppDbContext>()
                .ConexoesMercadoPago.AsNoTracking().ToListAsync());
        }

        public async Task ValidarPreservacaoAsync()
        {
            var row = await LerAsync();
            Assert.Equal(Before, row.TokensProtegidos);
            Assert.Equal(_previousExpiry, row.ExpiraEm);
            Assert.Equal(123456L, row.ContaId);
            Assert.Equal("origin-admin-renewal-test", row.AdministradorId);
            Assert.True(row.Sandbox);
        }

        public void Dispose()
        {
            Client.Dispose();
            Factory.Dispose();
            Provider.Dispose();
        }
    }

    private sealed class RelogioFixo : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => Now;
    }

    private sealed class FalhaGravacao(Cenario fixture) : SaveChangesInterceptor
    {
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData,
            InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            // A falha deste cenário é na substituição dos tokens, não no registro prévio de segurança.
            var trocaTokens = eventData.Context!.ChangeTracker.Entries<ConexaoMercadoPago>()
                .Any(entry => entry.Property(row => row.TokensProtegidos).IsModified);
            return fixture.FalharGravacao && trocaTokens
                ? throw new DbUpdateException("PRIVATE-FAILURE-ONLY-FOR-TESTS") : ValueTask.FromResult(result);
        }
    }

    private sealed class ProtecaoControlada(Cenario fixture) : IDataProtectionProvider
    {
        public IDataProtector CreateProtector(string purpose) => new Protector(fixture, fixture.Protection.CreateProtector(purpose));
        private sealed class Protector(Cenario fixture, IDataProtector inner) : IDataProtector
        {
            public IDataProtector CreateProtector(string purpose) => new Protector(fixture, inner.CreateProtector(purpose));
            public byte[] Protect(byte[] plaintext) => fixture.FalharProtecao
                ? throw new CryptographicException("PRIVATE-FAILURE-ONLY-FOR-TESTS") : inner.Protect(plaintext);
            public byte[] Unprotect(byte[] protectedData) => inner.Unprotect(protectedData);
        }
    }

    private sealed class ProvedorSimulado : HttpMessageHandler, IHttpClientFactory
    {
        public int Chamadas { get; private set; }
        public HttpStatusCode Status { get; set; } = HttpStatusCode.OK;
        public string Response { get; set; } = ValidResponse;
        public bool Timeout { get; set; }
        public HttpMethod? Method { get; private set; }
        public string? Url { get; private set; }
        public string? ContentType { get; private set; }
        public string? Body { get; private set; }
        public HttpClient CreateClient(string name) => new(this, disposeHandler: false);

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Chamadas++;
            Method = request.Method;
            Url = request.RequestUri?.AbsoluteUri;
            ContentType = request.Content?.Headers.ContentType?.MediaType;
            Body = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
            if (Timeout) throw new TaskCanceledException("PRIVATE-FAILURE-ONLY-FOR-TESTS");
            return new HttpResponseMessage(Status) { Content = new StringContent(Response, Encoding.UTF8, "application/json") };
        }
    }
}
