using System.Net.Http.Json;
using System.Text.Json;
using BarberApp.Application.Services;

namespace BarberApp.Infrastructure.Payment;

public sealed class MercadoPagoOAuthClient(IHttpClientFactory factory)
{
    internal async Task<TokensMercadoPago> TrocarAsync(string clientId, string clientSecret,
        string redirectUri, string code, string verifier, bool sandbox, CancellationToken cancellationToken)
    {
        try
        {
            using var client = factory.CreateClient("MercadoPagoOAuth");
            using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.mercadopago.com/oauth/token")
            {
                Content = JsonContent.Create(new
                {
                    client_id = clientId,
                    client_secret = clientSecret,
                    redirect_uri = redirectUri,
                    code,
                    code_verifier = verifier,
                    grant_type = "authorization_code",
                    test_token = sandbox
                })
            };
            using var response = await client.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
                throw new MercadoPagoOperacaoException("MercadoPagoProvedorFalhou");

            using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
            var root = body.RootElement;
            var accessToken = root.GetProperty("access_token").GetString();
            var refreshToken = root.GetProperty("refresh_token").GetString();
            var tokenType = root.GetProperty("token_type").GetString();
            var expiresIn = root.GetProperty("expires_in").GetInt32();
            var contaId = root.GetProperty("user_id").GetInt64();
            if (string.IsNullOrWhiteSpace(accessToken) || string.IsNullOrWhiteSpace(refreshToken)
                || !string.Equals(tokenType, "bearer", StringComparison.OrdinalIgnoreCase)
                || expiresIn <= 0 || expiresIn > 31_536_000 || contaId <= 0)
                throw new MercadoPagoOperacaoException("MercadoPagoRespostaInvalida");
            return new TokensMercadoPago(accessToken, refreshToken, contaId, expiresIn);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new MercadoPagoOperacaoException("MercadoPagoIndisponivel");
        }
        catch (HttpRequestException)
        {
            throw new MercadoPagoOperacaoException("MercadoPagoIndisponivel");
        }
        catch (Exception exception) when (exception is JsonException or KeyNotFoundException
            or InvalidOperationException or FormatException or OverflowException)
        {
            throw new MercadoPagoOperacaoException("MercadoPagoRespostaInvalida");
        }
    }
}
