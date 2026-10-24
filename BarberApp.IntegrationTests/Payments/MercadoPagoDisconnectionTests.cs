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

public sealed class MercadoPagoDisconnectionTests
{
    private const string BasePath = "/api/integracoes/mercado-pago";
    private const string Endpoint = BasePath + "/conexao";
    private const string OpaquePayload = "ONLY-FOR-TESTS-UNREADABLE-CIPHERTEXT";
    // Limite de diagnóstico; os sinais, não o tempo, ordenam as disputas.
    private static readonly TimeSpan Guard = TimeSpan.FromSeconds(10);

    [Theory]
    [InlineData("missing-key", 401)]
    [InlineData("invalid-key", 403)]
    [InlineData("missing-jwt", 401)]
    [InlineData("Cliente", 403)]
    [InlineData("Barbeiro", 403)]
    [InlineData("missing-id", 401)]
    public async Task Desconectar_DeveRecusarAcessoSemAlterarConexao_QuandoIdentidadeForInvalida(
        string identity, int status)
    {
        using var scenario = new Cenario();
        await scenario.GravarAsync();
        var pending = scenario.Iniciar("pending-admin");
        using var request = CriarRequest("conexao", identity);
        using var response = await scenario.Client.SendAsync(request);
        Assert.Equal((HttpStatusCode)status, response.StatusCode);
        Assert.True(await scenario.ExisteAsync());
        Assert.Equal(0, scenario.Chamadas);
        // Rejeição de acesso também não deve invalidar a autorização pendente.
        using var callback = await scenario.Client.GetAsync(Callback(pending));
        Assert.Equal(HttpStatusCode.OK, callback.StatusCode);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Desconectar_DeveRemoverCredenciaisSemDecifrarNemRevogar_QuandoAdminSolicitar(bool pending)
    {
        using var scenario = new Cenario();
        await scenario.GravarAsync(pending: pending);
        using var request = CriarRequest("conexao");
        using var response = await scenario.Client.SendAsync(request);
        await ValidarRemocaoAsync(response);
        Assert.False(await scenario.ExisteAsync());
        Assert.Equal(0, scenario.Chamadas);
        await ValidarNaoConectadoAsync(scenario);
        using var renewRequest = CriarRequest("renovacao");
        using var renew = await scenario.Client.SendAsync(renewRequest);
        await ValidarErroAsync(renew, HttpStatusCode.Conflict, "MercadoPagoNaoConectado");
        Assert.Equal(0, scenario.Chamadas);
    }

    [Fact]
    public async Task Desconectar_DeveSerIdempotente_QuandoInstalacaoJaEstiverSemConexao()
    {
        using var scenario = new Cenario();
        for (var attempt = 0; attempt < 2; attempt++)
        {
            using var request = CriarRequest("conexao");
            using var response = await scenario.Client.SendAsync(request);
            await ValidarRemocaoAsync(response);
        }
        Assert.Equal(0, scenario.Chamadas);
        await ValidarNaoConectadoAsync(scenario);
    }

    [Fact]
    public async Task Desconectar_DeveFuncionarSemConfiguracaoRemota_QuandoConexaoEstiverDesabilitada()
    {
        using var scenario = new Cenario(configurado: false);
        await scenario.GravarAsync();
        using var request = CriarRequest("conexao");
        using var response = await scenario.Client.SendAsync(request);
        await ValidarRemocaoAsync(response);
        Assert.False(await scenario.ExisteAsync());
        Assert.Equal(0, scenario.Chamadas);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Desconectar_DeveInvalidarTentativasDeTodosAdmins_EPermitirNovaAutorizacao(bool hasConnection)
    {
        using var scenario = new Cenario();
        if (hasConnection) await scenario.GravarAsync();
        var states = new[] { scenario.Iniciar("first-admin"), scenario.Iniciar("second-admin") };
        using var request = CriarRequest("conexao");
        using var response = await scenario.Client.SendAsync(request);
        await ValidarRemocaoAsync(response);
        foreach (var state in states)
        {
            using var callback = await scenario.Client.GetAsync(Callback(state));
            await ValidarErroAsync(callback, HttpStatusCode.BadRequest, "MercadoPagoCallbackInvalido");
        }
        Assert.Equal(0, scenario.Chamadas);
        Assert.False(await scenario.ExisteAsync());
        var fresh = scenario.Iniciar("new-admin");
        using var reconnect = await scenario.Client.GetAsync(Callback(fresh));
        Assert.Equal(HttpStatusCode.OK, reconnect.StatusCode);
        Assert.True(await scenario.ExisteAsync());
        Assert.Equal(1, scenario.Chamadas);
    }

    [Fact]
    public async Task Desconectar_DevePreservarConexaoETentativaELiberarExclusividade_QuandoGravacaoFalhar()
    {
        using var scenario = new Cenario();
        await scenario.GravarAsync();
        var state = scenario.Iniciar("pending-admin");
        scenario.FalharExclusao = true;
        using var request = CriarRequest("conexao");
        using var response = await scenario.Client.SendAsync(request);
        await ValidarErroAsync(response, HttpStatusCode.ServiceUnavailable, "MercadoPagoPersistenciaFalhou");
        using (var scope = scenario.Factory.Services.CreateScope())
        {
            var row = Assert.Single(await scope.ServiceProvider.GetRequiredService<AppDbContext>()
                .ConexoesMercadoPago.AsNoTracking().ToListAsync());
            Assert.Equal(OpaquePayload, row.TokensProtegidos);
            Assert.Equal("origin-admin", row.AdministradorId);
        }
        Assert.Equal(0, scenario.Chamadas);
        scenario.FalharExclusao = false;
        using var callback = await scenario.Client.GetAsync(Callback(state));
        Assert.Equal(HttpStatusCode.OK, callback.StatusCode);
        using var retryRequest = CriarRequest("conexao");
        using var retry = await scenario.Client.SendAsync(retryRequest);
        await ValidarRemocaoAsync(retry);
        Assert.False(await scenario.ExisteAsync());
    }

    [Theory]
    [InlineData("renovacao")]
    [InlineData("callback")]
    public async Task Desconectar_DeveRejeitarDisputa_EPermitirRemocaoAposOperacaoAdmitida(string operation)
    {
        using var scenario = new Cenario { PausarProvedor = true };
        await scenario.GravarAsync(validTokens: true);
        var state = scenario.Iniciar("reconnecting-admin");
        using var firstRequest = operation == "callback"
            ? new HttpRequestMessage(HttpMethod.Get, Callback(state)) : CriarRequest("renovacao");
        var firstTask = scenario.Client.SendAsync(firstRequest);
        try
        {
            await scenario.Entrou.Task.WaitAsync(Guard);
            using var request = CriarRequest("conexao");
            using var response = await scenario.Client.SendAsync(request).WaitAsync(Guard);
            await ValidarErroAsync(response, HttpStatusCode.Conflict, "MercadoPagoOperacaoEmAndamento");
            Assert.True(await scenario.ExisteAsync());
            Assert.Equal(1, scenario.Chamadas);
        }
        finally
        {
            scenario.Liberar.TrySetResult(true);
            using var first = await firstTask.WaitAsync(Guard);
            Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        }
        using var retryRequest = CriarRequest("conexao");
        using var retry = await scenario.Client.SendAsync(retryRequest);
        await ValidarRemocaoAsync(retry);
        Assert.False(await scenario.ExisteAsync());
        Assert.Equal(1, scenario.Chamadas);
    }

    [Fact]
    public async Task Desconectar_DeveBloquearAlteracoesEnquantoExclui_EInvalidarCallbackAposPersistir()
    {
        using var scenario = new Cenario { PausarExclusao = true };
        await scenario.GravarAsync(validTokens: true);
        var state = scenario.Iniciar("pending-admin");
        using var request = CriarRequest("conexao");
        var firstTask = scenario.Client.SendAsync(request);
        try
        {
            // Se a rota responder antes de atingir a escrita, mostra o status real, sem timeout artificial.
            await Task.WhenAny(scenario.Entrou.Task, firstTask).WaitAsync(Guard);
            if (firstTask.IsCompleted)
            {
                var early = await firstTask;
                Assert.Equal(HttpStatusCode.NoContent, early.StatusCode);
            }
            Assert.True(scenario.Entrou.Task.IsCompleted, "Exclusão deve alcançar a escrita controlada.");
            using var renewalRequest = CriarRequest("renovacao");
            using var renewal = await scenario.Client.SendAsync(renewalRequest).WaitAsync(Guard);
            await ValidarErroAsync(renewal, HttpStatusCode.Conflict, "MercadoPagoOperacaoEmAndamento");
            using var callback = await scenario.Client.GetAsync(Callback(state)).WaitAsync(Guard);
            await ValidarErroAsync(callback, HttpStatusCode.Conflict, "MercadoPagoOperacaoEmAndamento");
            using var startRequest = CriarRequest("autorizacao");
            using var start = await scenario.Client.SendAsync(startRequest).WaitAsync(Guard);
            await ValidarErroAsync(start, HttpStatusCode.Conflict, "MercadoPagoOperacaoEmAndamento");
            Assert.True(await scenario.ExisteAsync());
            Assert.Equal(0, scenario.Chamadas);
        }
        finally
        {
            scenario.Liberar.TrySetResult(true);
            using var first = await firstTask.WaitAsync(Guard);
        }
        using var replay = await scenario.Client.GetAsync(Callback(state));
        await ValidarErroAsync(replay, HttpStatusCode.BadRequest, "MercadoPagoCallbackInvalido");
        Assert.False(await scenario.ExisteAsync());
        Assert.Equal(0, scenario.Chamadas);
    }

    [Fact]
    public async Task Desconectar_DeveExigirApiKeyEJwtNoOpenApi()
    {
        using var scenario = new Cenario(development: true);
        using var response = await scenario.Client.GetAsync("/swagger/v1/swagger.json");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.True(json.RootElement.GetProperty("paths").TryGetProperty(Endpoint, out var endpoint),
            "OpenAPI deve declarar o endpoint de desconexão local.");
        var requirement = Assert.Single(endpoint.GetProperty("delete").GetProperty("security").EnumerateArray());
        Assert.True(requirement.TryGetProperty("ApiKey", out _));
        Assert.True(requirement.TryGetProperty("Bearer", out _));
    }

    private static string Callback(string state) => $"{BasePath}/callback?state={state}&code=ONLY-FOR-TESTS-CODE";

    private static async Task ValidarRemocaoAsync(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.True(response.Headers.CacheControl?.NoStore);
        Assert.Empty(await response.Content.ReadAsStringAsync());
    }

    private static async Task ValidarErroAsync(HttpResponseMessage response, HttpStatusCode status, string title)
    {
        Assert.Equal(status, response.StatusCode);
        Assert.True(response.Headers.CacheControl?.NoStore);
        var content = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("ONLY-FOR-TESTS-", content);
        Assert.DoesNotContain("TokensProtegidos", content);
        using var json = JsonDocument.Parse(content);
        Assert.Equal(title, json.RootElement.GetProperty("title").GetString());
    }

    private static async Task ValidarNaoConectadoAsync(Cenario scenario)
    {
        using var request = CriarRequest("status");
        using var response = await scenario.Client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.CacheControl?.NoStore);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(new[] { "contaId", "expiraEm", "sandbox", "status" },
            json.RootElement.EnumerateObject().Select(property => property.Name).OrderBy(name => name).ToArray());
        Assert.Equal("NaoConectado", json.RootElement.GetProperty("status").GetString());
        foreach (var field in new[] { "contaId", "expiraEm", "sandbox" })
            Assert.Equal(JsonValueKind.Null, json.RootElement.GetProperty(field).ValueKind);
    }

    private static HttpRequestMessage CriarRequest(string operation, string identity = "Admin")
    {
        var request = new HttpRequestMessage(operation == "conexao" ? HttpMethod.Delete
            : operation == "status" ? HttpMethod.Get : HttpMethod.Post, $"{BasePath}/{operation}");
        if (identity != "missing-key") request.Headers.Add("X-API-Key",
            identity == "invalid-key" ? "ONLY-FOR-TESTS-WRONG-KEY" : BarberAppApiFactory.ApiKey);
        if (identity == "missing-jwt") return request;
        var claims = new List<Claim> { new(ClaimTypes.Role, identity is "Cliente" or "Barbeiro" ? identity : "Admin") };
        if (identity != "missing-id") claims.Add(new Claim(ClaimTypes.NameIdentifier, "requesting-admin"));
        var jwt = new JwtSecurityToken(issuer: "BarberApp.IntegrationTests", audience: "BarberApp.IntegrationTests",
            claims: claims, expires: DateTime.UtcNow.AddMinutes(5),
            signingCredentials: new SigningCredentials(new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes("integration-tests-secret-key-with-at-least-32-characters")), SecurityAlgorithms.HmacSha256));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", new JwtSecurityTokenHandler().WriteToken(jwt));
        return request;
    }

    private sealed class Cenario : IDisposable
    {
        private readonly IDataProtectionProvider _protection = new EphemeralDataProtectionProvider();
        private readonly Provedor _provider;
        public WebApplicationFactory<BarberApp.API.Controllers.ServicosController> Factory { get; }
        public HttpClient Client { get; }
        public TaskCompletionSource<bool> Entrou { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource<bool> Liberar { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public bool FalharExclusao { get; set; }
        public bool PausarExclusao { get; set; }
        public bool PausarProvedor { get; set; }
        public int Chamadas;

        public Cenario(bool configurado = true, bool development = false)
        {
            _provider = new Provedor(this);
            var database = $"MercadoPagoDisconnection-{Guid.NewGuid()}";
            Factory = new BarberAppApiFactory(development ? "Development" : "Testing").WithWebHostBuilder(builder =>
            {
                builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(
                    new Dictionary<string, string?>
                    {
                        ["MercadoPago:ClientId"] = configurado ? "123456789" : null,
                        ["MercadoPago:ClientSecret"] = configurado ? "ONLY-FOR-TESTS-DISCONNECTION-SECRET" : null,
                        ["MercadoPago:RedirectUri"] = configurado ? "https://barberapp.example.test" + BasePath + "/callback" : null,
                        ["MercadoPago:ConexaoEnabled"] = configurado ? "true" : "false",
                        ["MercadoPago:Sandbox"] = configurado ? "true" : null
                    }));
                builder.ConfigureTestServices(services =>
                {
                    services.RemoveAll<AppDbContext>();
                    services.RemoveAll<DbContextOptions<AppDbContext>>();
                    services.RemoveAll<IDbContextOptionsConfiguration<AppDbContext>>();
                    services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase(database)
                        .AddInterceptors(new ExclusaoControlada(this)));
                    services.RemoveAll<IDataProtectionProvider>();
                    services.AddSingleton(_protection);
                    services.RemoveAll<IHttpClientFactory>();
                    services.AddSingleton<IHttpClientFactory>(_provider);
                });
            });
            Client = Factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        }

        public async Task GravarAsync(bool validTokens = false, bool pending = false)
        {
            var payload = validTokens ? _protection.CreateProtector("BarberApp.MercadoPago.Tokens.v1")
                .Protect(JsonSerializer.Serialize(new
                {
                    access_token = "ONLY-FOR-TESTS-ACCESS",
                    refresh_token = "ONLY-FOR-TESTS-REFRESH"
                })) : OpaquePayload;
            using var scope = Factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var row = new ConexaoMercadoPago(123456, "origin-admin", payload, DateTimeOffset.UtcNow.AddHours(1), true);
            if (pending) row.RegistrarPendenciaRenovacao();
            context.ConexoesMercadoPago.Add(row);
            await context.SaveChangesAsync();
        }

        public string Iniciar(string admin)
        {
            using var scope = Factory.Services.CreateScope();
            var response = scope.ServiceProvider.GetRequiredService<IConexaoMercadoPagoService>().Iniciar(admin);
            return QueryHelpers.ParseQuery(new Uri(response.UrlAutorizacao).Query)["state"].ToString();
        }

        public async Task<bool> ExisteAsync()
        {
            using var scope = Factory.Services.CreateScope();
            return await scope.ServiceProvider.GetRequiredService<AppDbContext>().ConexoesMercadoPago.AnyAsync();
        }

        public void Dispose()
        {
            Liberar.TrySetResult(true);
            Client.Dispose();
            Factory.Dispose();
            _provider.Dispose();
        }

        private sealed class ExclusaoControlada(Cenario scenario) : SaveChangesInterceptor
        {
            public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData,
                InterceptionResult<int> result, CancellationToken cancellationToken = default)
            {
                if (!eventData.Context!.ChangeTracker.Entries<ConexaoMercadoPago>()
                    .Any(entry => entry.State == EntityState.Deleted)) return result;
                if (scenario.PausarExclusao)
                {
                    scenario.Entrou.TrySetResult(true);
                    await scenario.Liberar.Task.WaitAsync(cancellationToken);
                }
                if (scenario.FalharExclusao) throw new DbUpdateException("ONLY-FOR-TESTS-DELETE-FAILURE");
                return result;
            }
        }

        private sealed class Provedor(Cenario scenario) : HttpMessageHandler, IHttpClientFactory
        {
            public HttpClient CreateClient(string name) => new(this, disposeHandler: false);

            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                Interlocked.Increment(ref scenario.Chamadas);
                if (scenario.PausarProvedor)
                {
                    scenario.Entrou.TrySetResult(true);
                    await scenario.Liberar.Task.WaitAsync(cancellationToken);
                }
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(JsonSerializer.Serialize(new
                    {
                        access_token = "ONLY-FOR-TESTS-NEW-ACCESS",
                        refresh_token = "ONLY-FOR-TESTS-NEW-REFRESH",
                        token_type = "bearer",
                        expires_in = 3600,
                        user_id = 123456
                    }), Encoding.UTF8, "application/json")
                };
            }
        }
    }
}
