using System.Security.Cryptography;
using System.Text;

namespace BarberApp.API.Middleware;

public sealed class ApiKeyAuthenticationMiddleware
{
    private const string ApiKeyHeaderName = "X-API-Key";
    private const string ApiKeyConfigurationKey = "ApiKey:Value";
    private readonly RequestDelegate _next;
    private readonly byte[] _expectedApiKey;

    public ApiKeyAuthenticationMiddleware(RequestDelegate next, IConfiguration configuration)
    {
        _next = next;

        var apiKey = configuration[ApiKeyConfigurationKey]!;
        _expectedApiKey = SHA256.HashData(Encoding.UTF8.GetBytes(apiKey));
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!context.Request.Headers.TryGetValue(ApiKeyHeaderName, out var informedApiKey))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Response.Headers.WWWAuthenticate = "ApiKey";
            return;
        }

        var informedApiKeyHash = SHA256.HashData(Encoding.UTF8.GetBytes(informedApiKey.ToString()));
        if (!CryptographicOperations.FixedTimeEquals(informedApiKeyHash, _expectedApiKey))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return;
        }

        await _next(context);
    }
}
