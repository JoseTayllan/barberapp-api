using System.Net;
using System.Text.Json;
using BarberApp.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace BarberApp.IntegrationTests.Authentication;

public sealed class ApiKeyAuthenticationTests : IClassFixture<BarberAppApiFactory>
{
    private const string ApiKeyHeaderName = "X-API-Key";
    private readonly HttpClient _client;

    public ApiKeyAuthenticationTests(BarberAppApiFactory factory)
    {
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    [Fact]
    public async Task Endpoint_DeveRetornar401_QuandoApiKeyNaoForInformada()
    {
        var response = await _client.GetAsync("/api/servicos");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Endpoint_DeveRetornar403_QuandoApiKeyForInvalida()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/servicos");
        request.Headers.Add(ApiKeyHeaderName, "api-key-invalida");

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task EndpointPublico_DeveSeguirFluxoNormal_QuandoApiKeyForValida()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/servicos");
        request.Headers.Add(ApiKeyHeaderName, BarberAppApiFactory.ApiKey);

        var response = await _client.SendAsync(request);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(JsonValueKind.Array, body.RootElement.ValueKind);
    }

    [Fact]
    public async Task ConsumidorJwt_DeveManterAutenticacaoExistente_QuandoApiKeyForValida()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/agendamentos");
        request.Headers.Add(ApiKeyHeaderName, BarberAppApiFactory.ApiKey);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
