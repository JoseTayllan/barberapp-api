namespace BarberApp.Infrastructure.Payment;

internal sealed class TokensMercadoPago(string accessToken, string refreshToken, long contaId, int expiresIn)
{
    public string AccessToken { get; } = accessToken;
    public string RefreshToken { get; } = refreshToken;
    public long ContaId { get; } = contaId;
    public int ExpiresIn { get; } = expiresIn;
}
