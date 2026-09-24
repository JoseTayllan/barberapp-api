using System.Net;
using System.Text.Json;
using BarberApp.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace BarberApp.IntegrationTests.Authentication;

public sealed class ApiKeyOpenApiTests : IDisposable
{
    private readonly BarberAppApiFactory _factory = new("Development");
    private readonly HttpClient _client;

    public ApiKeyOpenApiTests()
    {
        _client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    [Fact]
    public async Task Swagger_DeveSerPublicoEmDevelopment()
    {
        var response = await _client.GetAsync("/swagger/index.html");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task OpenApi_DeveDeclararEsquemaApiKeyNoHeaderEsperado()
    {
        using var document = await GetOpenApiDocumentAsync();
        var apiKeyScheme = document.RootElement
            .GetProperty("components")
            .GetProperty("securitySchemes")
            .GetProperty("ApiKey");

        Assert.Equal("apiKey", apiKeyScheme.GetProperty("type").GetString());
        Assert.Equal("X-API-Key", apiKeyScheme.GetProperty("name").GetString());
        Assert.Equal("header", apiKeyScheme.GetProperty("in").GetString());
    }

    [Fact]
    public async Task OpenApi_DeveExigirSomenteApiKey_EmEndpointPublico()
    {
        using var document = await GetOpenApiDocumentAsync();
        var operation = FindOperation(document.RootElement, "/api/servicos", "get");
        var security = GetEffectiveSecurity(document.RootElement, operation);
        var requirement = Assert.Single(security.EnumerateArray());

        Assert.True(requirement.TryGetProperty("ApiKey", out _));
        Assert.False(requirement.TryGetProperty("Bearer", out _));
        Assert.Single(requirement.EnumerateObject());
    }

    [Fact]
    public async Task OpenApi_DeveExigirApiKeyEBearer_EmEndpointAuthorize()
    {
        using var document = await GetOpenApiDocumentAsync();
        var operation = FindOperation(document.RootElement, "/api/agendamentos", "get");
        var security = GetEffectiveSecurity(document.RootElement, operation);
        var requirement = Assert.Single(security.EnumerateArray());

        Assert.True(requirement.TryGetProperty("ApiKey", out _));
        Assert.True(requirement.TryGetProperty("Bearer", out _));
        Assert.Equal(2, requirement.EnumerateObject().Count());
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    private async Task<JsonDocument> GetOpenApiDocumentAsync()
    {
        var response = await _client.GetAsync("/swagger/v1/swagger.json");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return JsonDocument.Parse(await response.Content.ReadAsStringAsync());
    }

    private static JsonElement FindOperation(JsonElement document, string expectedPath, string method)
    {
        var path = document.GetProperty("paths")
            .EnumerateObject()
            .Single(candidate => string.Equals(
                candidate.Name,
                expectedPath,
                StringComparison.OrdinalIgnoreCase));

        return path.Value.GetProperty(method);
    }

    private static JsonElement GetEffectiveSecurity(JsonElement document, JsonElement operation)
    {
        return operation.TryGetProperty("security", out var operationSecurity)
            ? operationSecurity
            : document.GetProperty("security");
    }
}
