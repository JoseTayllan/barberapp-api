using System.Net;
using System.Text.Json;
using BarberApp.Application.Services;
using BarberApp.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BarberApp.IntegrationTests.Payments;

public sealed class MercadoPagoCallbackTests
{
    private const string Endpoint = "/api/integracoes/mercado-pago/callback";

    [Theory]
    [InlineData("")]
    [InlineData("?state=unknown&error=access_denied")]
    [InlineData("?state=%20&code=FAKE-CODE")]
    [InlineData("?state=../../invalid&code=FAKE-CODE")]
    public async Task Callback_DeveRejeitarStateInvalido_SemApiKeyOuJwt(string query)
    {
        using var factory = CriarFactory();
        using var client = CriarClient(factory);
        using var response = await client.GetAsync(Endpoint + query);
        await ValidarErroAsync(response, HttpStatusCode.BadRequest, "MercadoPagoCallbackInvalido");
    }

    [Fact]
    public async Task Callback_DeveAceitarRecusaUmaUnicaVez_SemApiKeyOuJwt()
    {
        using var factory = CriarFactory();
        using var client = CriarClient(factory);
        var state = Iniciar(factory);
        using var response = await client.GetAsync(Recusa(state));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.CacheControl?.NoStore);
        Assert.Equal("no-referrer", response.Headers.GetValues("Referrer-Policy").Single());
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("AutorizacaoRecusada", body.RootElement.GetProperty("status").GetString());
        using var replay = await client.GetAsync(Recusa(state));
        await ValidarErroAsync(replay, HttpStatusCode.BadRequest, "MercadoPagoCallbackInvalido");
    }

    [Theory]
    [InlineData(10)]
    [InlineData(11)]
    public async Task Callback_DeveRejeitarTentativaExpirada_SemEsperarRelogioReal(int minutos)
    {
        var clock = new RelogioControlado();
        using var factory = CriarFactory(clock);
        using var client = CriarClient(factory);
        var state = Iniciar(factory);
        clock.Avancar(TimeSpan.FromMinutes(minutos));
        using var response = await client.GetAsync(Recusa(state));
        await ValidarErroAsync(response, HttpStatusCode.BadRequest, "MercadoPagoCallbackInvalido");
    }

    [Fact]
    public async Task Callback_DeveInvalidarTentativaAnterior_QuandoMesmoAdminReiniciar()
    {
        using var factory = CriarFactory();
        using var client = CriarClient(factory);
        var anterior = Iniciar(factory);
        var atual = Iniciar(factory);
        using var rejeitado = await client.GetAsync(Recusa(anterior));
        await ValidarErroAsync(rejeitado, HttpStatusCode.BadRequest, "MercadoPagoCallbackInvalido");
        using var aceito = await client.GetAsync(Recusa(atual));
        Assert.Equal(HttpStatusCode.OK, aceito.StatusCode);
    }

    [Fact]
    public async Task Callback_DeveManterTentativasIndependentes_QuandoAdminsForemDiferentes()
    {
        using var factory = CriarFactory();
        using var client = CriarClient(factory);
        var primeiro = Iniciar(factory, "primeiro-admin");
        var segundo = Iniciar(factory, "segundo-admin");
        using var response1 = await client.GetAsync(Recusa(primeiro));
        using var response2 = await client.GetAsync(Recusa(segundo));
        Assert.Equal(HttpStatusCode.OK, response1.StatusCode);
        Assert.Equal(HttpStatusCode.OK, response2.StatusCode);
    }

    [Fact]
    public async Task Callback_DeveAceitarApenasUmaRequisicao_QuandoHouverConcorrencia()
    {
        using var factory = CriarFactory();
        using var client = CriarClient(factory);
        var state = Iniciar(factory);
        var responses = await Task.WhenAll(Enumerable.Range(0, 8).Select(_ => client.GetAsync(Recusa(state))));
        try
        {
            Assert.Single(responses, response => response.StatusCode == HttpStatusCode.OK);
            Assert.Equal(7, responses.Count(response => response.StatusCode == HttpStatusCode.BadRequest));
        }
        finally
        {
            foreach (var response in responses) response.Dispose();
        }
    }

    [Theory]
    [InlineData("")]
    [InlineData("&error=unexpected-provider-detail")]
    [InlineData("&code=FAKE-CODE&error=access_denied")]
    [InlineData("&error=access_denied&state=other-state")]
    [InlineData("&code=first&code=second")]
    public async Task Callback_DeveRejeitarParametrosAmbiguos_SemRevelarValores(string query)
    {
        using var factory = CriarFactory();
        using var client = CriarClient(factory);
        var state = Iniciar(factory);
        using var response = await client.GetAsync($"{Endpoint}?state={state}{query}");
        await ValidarErroAsync(response, HttpStatusCode.BadRequest, "MercadoPagoCallbackInvalido");
        var content = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain(state, content);
        Assert.DoesNotContain("unexpected-provider-detail", content);
    }

    [Fact]
    public async Task Callback_DeveFalharSeguramente_SemSimularConexaoQuandoCodigoForValido()
    {
        using var factory = CriarFactory();
        using var client = CriarClient(factory);
        var state = Iniciar(factory);
        using var response = await client.GetAsync($"{Endpoint}?state={state}&code=FAKE-CODE-NOT-A-SECRET");
        await ValidarErroAsync(response, HttpStatusCode.ServiceUnavailable, "MercadoPagoTrocaTokenPendente");
        Assert.DoesNotContain("FAKE-CODE", await response.Content.ReadAsStringAsync());
        using var replay = await client.GetAsync(Recusa(state));
        await ValidarErroAsync(replay, HttpStatusCode.BadRequest, "MercadoPagoCallbackInvalido");
    }

    [Theory]
    [InlineData("/api/servicos", "GET")]
    [InlineData("/api/integracoes/mercado-pago/autorizacao", "POST")]
    [InlineData("/api/integracoes/mercado-pago/callback-extra", "GET")]
    [InlineData(Endpoint, "POST")]
    public async Task ApiKey_DeveContinuarObrigatoria_ForaDoGetCallback(string path, string method)
    {
        using var factory = CriarFactory();
        using var client = CriarClient(factory);
        using var request = new HttpRequestMessage(new HttpMethod(method), path);
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task OpenApi_DeveDeclararCallbackSemApiKey_EnquantoInicioExigeApiKeyEBearer()
    {
        using var factory = new BarberAppApiFactory("Development");
        using var client = CriarClient(factory);
        using var document = JsonDocument.Parse(await client.GetStringAsync("/swagger/v1/swagger.json"));
        var paths = document.RootElement.GetProperty("paths");
        var callbackSecurity = paths.GetProperty(Endpoint).GetProperty("get").GetProperty("security");
        var callbackRequirement = Assert.Single(callbackSecurity.EnumerateArray());
        Assert.Empty(callbackRequirement.EnumerateObject());
        var security = paths.GetProperty("/api/integracoes/mercado-pago/autorizacao")
            .GetProperty("post").GetProperty("security");
        var requirement = Assert.Single(security.EnumerateArray());
        Assert.True(requirement.TryGetProperty("ApiKey", out _));
        Assert.True(requirement.TryGetProperty("Bearer", out _));
    }

    private static async Task ValidarErroAsync(HttpResponseMessage response, HttpStatusCode status, string title)
    {
        Assert.Equal(status, response.StatusCode);
        Assert.True(response.Headers.CacheControl?.NoStore);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(title, body.RootElement.GetProperty("title").GetString());
    }

    private static string Recusa(string state) => $"{Endpoint}?state={state}&error=access_denied";

    private static string Iniciar(WebApplicationFactory<BarberApp.API.Controllers.ServicosController> factory,
        string admin = "admin-callback-test")
    {
        using var scope = factory.Services.CreateScope();
        var response = scope.ServiceProvider.GetRequiredService<IConexaoMercadoPagoService>().Iniciar(admin);
        return QueryHelpers.ParseQuery(new Uri(response.UrlAutorizacao).Query)["state"].ToString();
    }

    private static WebApplicationFactory<BarberApp.API.Controllers.ServicosController> CriarFactory(
        RelogioControlado? clock = null) => new BarberAppApiFactory().WithWebHostBuilder(builder =>
    {
        var values = new Dictionary<string, string?>
        {
            ["MercadoPago:ClientId"] = "123456789",
            ["MercadoPago:ClientSecret"] = "ONLY-FOR-TESTS-NOT-A-PROVIDER-CREDENTIAL",
            ["MercadoPago:RedirectUri"] = "https://barberapp.example.test" + Endpoint
        };
        foreach (var value in values) builder.UseSetting(value.Key, value.Value);
        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(values));
        builder.ConfigureServices(services => services.AddSingleton<TimeProvider>(clock ?? new RelogioControlado()));
    });

    private static HttpClient CriarClient(WebApplicationFactory<BarberApp.API.Controllers.ServicosController> factory) =>
        factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

    private sealed class RelogioControlado : TimeProvider
    {
        private DateTimeOffset _agora = new(2030, 1, 1, 0, 0, 0, TimeSpan.Zero);
        public override DateTimeOffset GetUtcNow() => _agora;
        public void Avancar(TimeSpan intervalo) => _agora += intervalo;
    }
}
