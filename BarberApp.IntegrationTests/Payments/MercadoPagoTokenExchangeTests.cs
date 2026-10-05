using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using BarberApp.Application.Services;
using BarberApp.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace BarberApp.IntegrationTests.Payments;

public sealed class MercadoPagoTokenExchangeTests
{
    private const string Endpoint = "/api/integracoes/mercado-pago/callback";
    private const string FakeCode = "ONLY-FOR-TESTS-AUTHORIZATION-CODE";
    private const string FakeSecret = "ONLY-FOR-TESTS-CLIENT-SECRET";
    private const string FakeAccessToken = "ONLY-FOR-TESTS-ACCESS-TOKEN";
    private const string FakeRefreshToken = "ONLY-FOR-TESTS-REFRESH-TOKEN";
    private const string ValidResponse = """
        {"access_token":"ONLY-FOR-TESTS-ACCESS-TOKEN","refresh_token":"ONLY-FOR-TESTS-REFRESH-TOKEN",
         "token_type":"bearer","expires_in":3600,"user_id":123456,"scope":"offline_access"}
        """;

    [Fact]
    public async Task Callback_DeveTrocarCodigoComPkceEConectar_SemDevolverSegredos()
    {
        using var provider = new ProvedorSimulado();
        using var factory = CriarFactory(provider);
        using var client = CriarClient(factory);
        var tentativa = Iniciar(factory);

        using var response = await client.GetAsync(Retorno(tentativa.State));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        using var body = JsonDocument.Parse(content);
        Assert.Equal("Conectado", body.RootElement.GetProperty("status").GetString());
        ValidarAusenciaDeSegredos(content);
        Assert.True(response.Headers.CacheControl?.NoStore);
        Assert.Equal("no-referrer", response.Headers.GetValues("Referrer-Policy").Single());
        Assert.Equal(1, provider.Chamadas);
        Assert.Equal(HttpMethod.Post, provider.Method);
        Assert.Equal("https://api.mercadopago.com/oauth/token", provider.Url);
        Assert.Equal("application/json", provider.ContentType);
        using var sent = JsonDocument.Parse(provider.Body!);
        var root = sent.RootElement;
        Assert.Equal("123456789", root.GetProperty("client_id").GetString());
        Assert.Equal(FakeSecret, root.GetProperty("client_secret").GetString());
        Assert.Equal(FakeCode, root.GetProperty("code").GetString());
        Assert.Equal("authorization_code", root.GetProperty("grant_type").GetString());
        Assert.Equal("https://barberapp.example.test" + Endpoint, root.GetProperty("redirect_uri").GetString());
        Assert.True(root.GetProperty("test_token").GetBoolean());
        var verifier = root.GetProperty("code_verifier").GetString()!;
        Assert.Matches("^[A-Za-z0-9_-]{43}$", verifier);
        var challenge = Convert.ToBase64String(SHA256.HashData(Encoding.ASCII.GetBytes(verifier)))
            .TrimEnd('=').Replace('+', '-').Replace('/', '_');
        Assert.Equal(tentativa.Challenge, challenge);
        Assert.DoesNotContain(verifier, content);
    }

    [Theory]
    [InlineData(400)]
    [InlineData(401)]
    [InlineData(429)]
    [InlineData(500)]
    public async Task Callback_DeveTraduzirFalhaDoProvedor_SemRevelarCorpoOuRepetirCodigo(int status)
    {
        using var provider = new ProvedorSimulado((HttpStatusCode)status,
            "{\"error\":\"PRIVATE-PROVIDER-DETAIL-ONLY-FOR-TESTS\"}");
        using var factory = CriarFactory(provider);
        using var client = CriarClient(factory);
        var tentativa = Iniciar(factory);

        using var response = await client.GetAsync(Retorno(tentativa.State));

        await ValidarErroAsync(response, HttpStatusCode.BadGateway, "MercadoPagoProvedorFalhou");
        Assert.Equal(1, provider.Chamadas);
        using var replay = await client.GetAsync(Retorno(tentativa.State));
        Assert.Equal(HttpStatusCode.BadRequest, replay.StatusCode);
        Assert.Equal(1, provider.Chamadas);
    }

    [Theory]
    [InlineData("not-json")]
    [InlineData("{}")]
    [InlineData("{\"access_token\":\"\",\"refresh_token\":\"fake\",\"expires_in\":3600,\"user_id\":123,\"token_type\":\"bearer\"}")]
    [InlineData("{\"access_token\":\"fake\",\"expires_in\":3600,\"user_id\":123,\"token_type\":\"bearer\"}")]
    [InlineData("{\"access_token\":\"fake\",\"refresh_token\":\"fake\",\"expires_in\":0,\"user_id\":123,\"token_type\":\"bearer\"}")]
    [InlineData("{\"access_token\":\"fake\",\"refresh_token\":\"fake\",\"expires_in\":3600,\"user_id\":0,\"token_type\":\"bearer\"}")]
    [InlineData("{\"access_token\":\"fake\",\"refresh_token\":\"fake\",\"expires_in\":3600,\"user_id\":123,\"token_type\":\"unexpected\"}")]
    public async Task Callback_DeveRejeitarRespostaIncompleta_SemDeclararConexao(string json)
    {
        using var provider = new ProvedorSimulado(HttpStatusCode.OK, json);
        using var factory = CriarFactory(provider);
        using var client = CriarClient(factory);
        var tentativa = Iniciar(factory);
        using var response = await client.GetAsync(Retorno(tentativa.State));
        await ValidarErroAsync(response, HttpStatusCode.BadGateway, "MercadoPagoRespostaInvalida");
        Assert.Equal(1, provider.Chamadas);
    }

    [Fact]
    public async Task Callback_DeveTratarTimeout_SemRetryAutomatico()
    {
        using var provider = new ProvedorSimulado(timeout: true);
        using var factory = CriarFactory(provider);
        using var client = CriarClient(factory);
        var tentativa = Iniciar(factory);
        using var response = await client.GetAsync(Retorno(tentativa.State));
        await ValidarErroAsync(response, HttpStatusCode.ServiceUnavailable, "MercadoPagoIndisponivel");
        Assert.Equal(1, provider.Chamadas);
    }

    [Fact]
    public async Task Callback_DeveConsumirCodigoUmaVez_QuandoProvedorResponderComSucesso()
    {
        using var provider = new ProvedorSimulado();
        using var factory = CriarFactory(provider);
        using var client = CriarClient(factory);
        var tentativa = Iniciar(factory);
        using var first = await client.GetAsync(Retorno(tentativa.State));
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        using var replay = await client.GetAsync(Retorno(tentativa.State));
        Assert.Equal(HttpStatusCode.BadRequest, replay.StatusCode);
        Assert.Equal(1, provider.Chamadas);
    }

    [Fact]
    public async Task Callback_DeveEvitarContatoExterno_QuandoStateForDesconhecido()
    {
        using var provider = new ProvedorSimulado();
        using var factory = CriarFactory(provider);
        using var client = CriarClient(factory);
        using var response = await client.GetAsync(Retorno(new string('A', 43)));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(0, provider.Chamadas);
    }

    [Fact]
    public async Task Callback_DeveEvitarContatoExterno_QuandoDonoRecusar()
    {
        using var provider = new ProvedorSimulado();
        using var factory = CriarFactory(provider);
        using var client = CriarClient(factory);
        var tentativa = Iniciar(factory);
        using var response = await client.GetAsync($"{Endpoint}?state={tentativa.State}&error=access_denied");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(0, provider.Chamadas);
    }

    private static string Retorno(string state) => $"{Endpoint}?state={state}&code={FakeCode}";

    private static (string State, string Challenge) Iniciar(
        WebApplicationFactory<BarberApp.API.Controllers.ServicosController> factory)
    {
        using var scope = factory.Services.CreateScope();
        var response = scope.ServiceProvider.GetRequiredService<IConexaoMercadoPagoService>().Iniciar("admin-token-test");
        var query = QueryHelpers.ParseQuery(new Uri(response.UrlAutorizacao).Query);
        return (query["state"].ToString(), query["code_challenge"].ToString());
    }

    private static async Task ValidarErroAsync(HttpResponseMessage response, HttpStatusCode status, string title)
    {
        Assert.Equal(status, response.StatusCode);
        Assert.True(response.Headers.CacheControl?.NoStore);
        var content = await response.Content.ReadAsStringAsync();
        using var body = JsonDocument.Parse(content);
        Assert.Equal(title, body.RootElement.GetProperty("title").GetString());
        ValidarAusenciaDeSegredos(content);
        Assert.DoesNotContain("PRIVATE-PROVIDER-DETAIL", content);
    }

    private static void ValidarAusenciaDeSegredos(string content)
    {
        Assert.DoesNotContain(FakeCode, content);
        Assert.DoesNotContain(FakeSecret, content);
        Assert.DoesNotContain(FakeAccessToken, content);
        Assert.DoesNotContain(FakeRefreshToken, content);
        Assert.DoesNotContain("access_token", content);
        Assert.DoesNotContain("refresh_token", content);
        Assert.DoesNotContain("code_verifier", content);
    }

    private static WebApplicationFactory<BarberApp.API.Controllers.ServicosController> CriarFactory(
        ProvedorSimulado provider) => new BarberAppApiFactory().WithWebHostBuilder(builder =>
    {
        var values = new Dictionary<string, string?>
        {
            ["MercadoPago:ClientId"] = "123456789",
            ["MercadoPago:ClientSecret"] = FakeSecret,
            ["MercadoPago:RedirectUri"] = "https://barberapp.example.test" + Endpoint,
            ["MercadoPago:ConexaoEnabled"] = "true",
            ["MercadoPago:Sandbox"] = "true"
        };
        foreach (var setting in values) builder.UseSetting(setting.Key, setting.Value);
        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(values));
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IHttpClientFactory>();
            services.AddSingleton<IHttpClientFactory>(provider);
        });
    });

    private static HttpClient CriarClient(WebApplicationFactory<BarberApp.API.Controllers.ServicosController> factory) =>
        factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

    // Nenhum socket é aberto: todos os HttpClients externos usam este handler.
    private sealed class ProvedorSimulado(
        HttpStatusCode status = HttpStatusCode.OK, string response = ValidResponse, bool timeout = false)
        : HttpMessageHandler, IHttpClientFactory
    {
        private int _chamadas;
        public int Chamadas => _chamadas;
        public HttpMethod? Method { get; private set; }
        public string? Url { get; private set; }
        public string? ContentType { get; private set; }
        public string? Body { get; private set; }

        public HttpClient CreateClient(string name) => new(this, disposeHandler: false);

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref _chamadas);
            Method = request.Method;
            Url = request.RequestUri?.AbsoluteUri;
            ContentType = request.Content?.Headers.ContentType?.MediaType;
            Body = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
            if (timeout) throw new TaskCanceledException("ONLY-FOR-TESTS-TIMEOUT");
            return new HttpResponseMessage(status)
            {
                Content = new StringContent(response, Encoding.UTF8, "application/json")
            };
        }
    }
}
