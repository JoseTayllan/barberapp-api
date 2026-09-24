using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BarberApp.API.Controllers;
using BarberApp.API.Middleware;
using BarberApp.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace BarberApp.IntegrationTests.Authentication;

public sealed class ApiKeyAuthenticationRiskTests : IClassFixture<BarberAppApiFactory>
{
    private const string ApiKeyHeaderName = "X-API-Key";
    private readonly HttpClient _client;

    public ApiKeyAuthenticationRiskTests(BarberAppApiFactory factory)
    {
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    [Fact]
    public void Aplicacao_DeveFalharNaInicializacao_QuandoApiKeyNaoForConfigurada()
    {
        using var factory = new ApiKeyConfigurationFactory(apiKey: null);

        var exception = Assert.ThrowsAny<Exception>(() => factory.CreateClient());

        Assert.Contains(
            "A configuração 'ApiKey:Value' deve conter uma API key.",
            GetExceptionMessages(exception));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t")]
    public void Aplicacao_DeveFalharNaInicializacao_QuandoApiKeyForVaziaOuWhitespace(string apiKey)
    {
        using var factory = new ApiKeyConfigurationFactory(apiKey);

        var exception = Assert.ThrowsAny<Exception>(() => factory.CreateClient());

        Assert.Contains(
            "A configuração 'ApiKey:Value' deve conter uma API key.",
            GetExceptionMessages(exception));
    }

    [Fact]
    public async Task Endpoint_DeveInformarChallengeApiKey_QuandoApiKeyNaoForInformada()
    {
        var response = await _client.GetAsync("/api/servicos");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var challenge = Assert.Single(response.Headers.WwwAuthenticate);
        Assert.Equal("ApiKey", challenge.Scheme);
        Assert.Null(challenge.Parameter);
    }

    [Fact]
    public async Task Middleware_DeveRetornar403_QuandoHeaderApiKeyForVazio()
    {
        var nextWasCalled = false;
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ApiKey:Value"] = BarberAppApiFactory.ApiKey
            })
            .Build();
        var middleware = new ApiKeyAuthenticationMiddleware(
            _ =>
            {
                nextWasCalled = true;
                return Task.CompletedTask;
            },
            configuration);
        var context = new DefaultHttpContext();
        context.Request.Headers[ApiKeyHeaderName] = string.Empty;

        await middleware.InvokeAsync(context);

        Assert.Equal(StatusCodes.Status403Forbidden, context.Response.StatusCode);
        Assert.False(nextWasCalled);
    }

    [Theory]
    [InlineData(BarberAppApiFactory.ApiKey, BarberAppApiFactory.ApiKey)]
    [InlineData(BarberAppApiFactory.ApiKey, "api-key-invalida")]
    public async Task Endpoint_DeveRetornar403_QuandoHeaderApiKeyForMultiplo(
        string firstValue,
        string secondValue)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/servicos");
        Assert.True(request.Headers.TryAddWithoutValidation(
            ApiKeyHeaderName,
            new[] { firstValue, secondValue }));

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task PreflightCors_DeveSerAtendido_QuandoApiKeyNaoForInformada()
    {
        using var request = new HttpRequestMessage(HttpMethod.Options, "/api/servicos");
        request.Headers.Add("Origin", "http://localhost:3000");
        request.Headers.Add("Access-Control-Request-Method", "GET");

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(
            "http://localhost:3000",
            Assert.Single(response.Headers.GetValues("Access-Control-Allow-Origin")));
    }

    [Fact]
    public async Task RotaInexistente_DeveRetornar401_QuandoApiKeyNaoForInformada()
    {
        var response = await _client.GetAsync("/api/rota-inexistente");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("ApiKey", Assert.Single(response.Headers.WwwAuthenticate).Scheme);
    }

    [Fact]
    public async Task RotaInexistente_DeveRetornar404_QuandoApiKeyForValida()
    {
        using var request = CreateRequestWithApiKey(HttpMethod.Get, "/api/rota-inexistente");

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Login_DeveRetornar401DoMiddleware_QuandoApiKeyNaoForInformada()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/auth/login",
            new { email = "inexistente@barberapp.test", password = "senha-invalida" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("ApiKey", Assert.Single(response.Headers.WwwAuthenticate).Scheme);
        Assert.Equal(string.Empty, await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Login_DeveAlcancarFluxoNormal_QuandoApiKeyForValida()
    {
        using var request = CreateRequestWithApiKey(HttpMethod.Post, "/api/auth/login");
        request.Content = JsonContent.Create(
            new { email = "inexistente@barberapp.test", password = "senha-invalida" });

        var response = await _client.SendAsync(request);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Empty(response.Headers.WwwAuthenticate);
        Assert.Equal(
            "E-mail ou senha inválidos.",
            body.RootElement.GetProperty("mensagem").GetString());
    }

    private static HttpRequestMessage CreateRequestWithApiKey(HttpMethod method, string path)
    {
        var request = new HttpRequestMessage(method, path);
        request.Headers.Add(ApiKeyHeaderName, BarberAppApiFactory.ApiKey);
        return request;
    }

    private static string GetExceptionMessages(Exception exception)
    {
        var messages = new List<string>();

        for (var current = exception; current is not null; current = current.InnerException)
        {
            messages.Add(current.Message);
        }

        return string.Join(Environment.NewLine, messages);
    }

    private sealed class ApiKeyConfigurationFactory(string? apiKey)
        : WebApplicationFactory<ServicosController>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");

            if (apiKey is not null)
            {
                builder.UseSetting("ApiKey:Value", apiKey);
            }
        }
    }
}
