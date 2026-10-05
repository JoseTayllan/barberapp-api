using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using BarberApp.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace BarberApp.IntegrationTests.Payments;

public sealed class MercadoPagoConnectionTests
{
    private const string Endpoint = "/api/integracoes/mercado-pago/autorizacao";
    private const string TestSecret = "ONLY-FOR-TESTS-NOT-A-PROVIDER-CREDENTIAL";
    private const string Callback = "https://barberapp.example.test/api/integracoes/mercado-pago/callback";

    [Fact]
    public async Task Iniciar_DeveRetornar401_QuandoApiKeyEstiverAusente()
    {
        using var factory = CriarFactory();
        using var client = CriarClient(factory);
        using var request = CriarRequest("Admin");
        request.Headers.Remove("X-API-Key");

        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Iniciar_DeveRetornar401_QuandoJwtEstiverAusente()
    {
        using var factory = CriarFactory();
        using var client = CriarClient(factory);
        using var request = CriarRequest(null);

        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData("Cliente")]
    [InlineData("Barbeiro")]
    public async Task Iniciar_DeveRetornar403_QuandoUsuarioNaoForAdmin(string role)
    {
        using var factory = CriarFactory();
        using var client = CriarClient(factory);
        using var request = CriarRequest(role);

        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Iniciar_DeveRetornar401_QuandoAdminNaoTiverIdentificador()
    {
        using var factory = CriarFactory();
        using var client = CriarClient(factory);
        using var request = CriarRequest("Admin", incluirIdentificador: false);

        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Iniciar_DeveGerarUrlOAuthComStateEPkceSemSegredos_QuandoAdminForValido()
    {
        using var factory = CriarFactory();
        using var client = CriarClient(factory);
        using var request = CriarRequest("Admin");

        using var response = await client.SendAsync(request);
        var content = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.CacheControl?.NoStore);
        using var body = JsonDocument.Parse(content);
        var url = new Uri(body.RootElement.GetProperty("urlAutorizacao").GetString()!);
        var query = QueryHelpers.ParseQuery(url.Query);

        Assert.Equal("https", url.Scheme);
        Assert.Equal("auth.mercadopago.com", url.Host);
        Assert.Equal("/authorization", url.AbsolutePath);
        Assert.Equal("code", query["response_type"].ToString());
        Assert.Equal("123456789", query["client_id"].ToString());
        Assert.Equal("mp", query["platform_id"].ToString());
        Assert.Equal(Callback, query["redirect_uri"].ToString());
        Assert.Matches("^[A-Za-z0-9_-]{43}$", query["state"].ToString());
        Assert.Matches("^[A-Za-z0-9_-]{43}$", query["code_challenge"].ToString());
        Assert.Equal("S256", query["code_challenge_method"].ToString());
        Assert.DoesNotContain(TestSecret, content, StringComparison.Ordinal);
        Assert.False(query.ContainsKey("client_secret"));
        Assert.False(query.ContainsKey("code_verifier"));
        var expiraEm = body.RootElement.GetProperty("expiraEm").GetDateTimeOffset();
        Assert.InRange(expiraEm, DateTimeOffset.UtcNow.AddMinutes(9), DateTimeOffset.UtcNow.AddMinutes(11));
    }

    [Fact]
    public async Task Iniciar_DeveGerarStatesDiferentes_QuandoChamadoDuasVezes()
    {
        using var factory = CriarFactory();
        using var client = CriarClient(factory);
        var states = new List<string>();
        for (var attempt = 0; attempt < 2; attempt++)
        {
            using var request = CriarRequest("Admin");
            using var response = await client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            var url = new Uri(body.RootElement.GetProperty("urlAutorizacao").GetString()!);
            states.Add(QueryHelpers.ParseQuery(url.Query)["state"].ToString());
        }

        Assert.NotEqual(states[0], states[1]);
    }

    [Theory]
    [InlineData("ClientId", null)]
    [InlineData("ClientId", "   ")]
    [InlineData("ClientId", "not-a-numeric-id")]
    [InlineData("ClientSecret", null)]
    [InlineData("RedirectUri", null)]
    [InlineData("RedirectUri", "http://public.example.test/callback")]
    [InlineData("RedirectUri", "https://user:password@example.test/callback")]
    [InlineData("RedirectUri", "https://example.test/callback#fragment")]
    public async Task Iniciar_DeveRetornar503SemDetalhes_QuandoConfiguracaoForInvalida(string key, string? value)
    {
        using var factory = CriarFactory(key, value);
        using var client = CriarClient(factory);
        using var request = CriarRequest("Admin");

        using var response = await client.SendAsync(request);
        var content = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        using var body = JsonDocument.Parse(content);
        Assert.Equal("MercadoPagoNaoConfigurado", body.RootElement.GetProperty("title").GetString());
        Assert.DoesNotContain(TestSecret, content, StringComparison.Ordinal);
        Assert.DoesNotContain("ClientSecret", content, StringComparison.Ordinal);
        if (!string.IsNullOrWhiteSpace(value))
            Assert.DoesNotContain(value, content, StringComparison.Ordinal);
    }

    private static WebApplicationFactory<BarberApp.API.Controllers.ServicosController> CriarFactory(
        string? key = null, string? value = null)
    {
        return new BarberAppApiFactory().WithWebHostBuilder(builder =>
        {
            var values = new Dictionary<string, string?>
            {
                ["MercadoPago:ClientId"] = "123456789",
                ["MercadoPago:ClientSecret"] = TestSecret,
                ["MercadoPago:RedirectUri"] = Callback
            };
            if (key is not null) values[$"MercadoPago:{key}"] = value;
            foreach (var setting in values.Where(item => item.Value is not null))
                builder.UseSetting(setting.Key, setting.Value);
            builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(values));
        });
    }

    private static HttpClient CriarClient(WebApplicationFactory<BarberApp.API.Controllers.ServicosController> factory) =>
        factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

    private static HttpRequestMessage CriarRequest(string? role, bool incluirIdentificador = true)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, Endpoint);
        request.Headers.Add("X-API-Key", BarberAppApiFactory.ApiKey);
        if (role is not null)
        {
            var claims = new List<Claim> { new(ClaimTypes.Role, role) };
            if (incluirIdentificador) claims.Add(new Claim(ClaimTypes.NameIdentifier, "admin-test-id"));
            var jwt = new JwtSecurityToken(
                issuer: "BarberApp.IntegrationTests", audience: "BarberApp.IntegrationTests",
                claims: claims, expires: DateTime.UtcNow.AddMinutes(5),
                signingCredentials: new SigningCredentials(new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes("integration-tests-secret-key-with-at-least-32-characters")),
                    SecurityAlgorithms.HmacSha256));
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", new JwtSecurityTokenHandler().WriteToken(jwt));
        }
        return request;
    }
}
