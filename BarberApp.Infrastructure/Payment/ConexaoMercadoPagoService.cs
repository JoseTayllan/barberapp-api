using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using BarberApp.Application.DTOs;
using BarberApp.Application.Services;
using BarberApp.Domain.Entities;
using BarberApp.Domain.Interfaces;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace BarberApp.Infrastructure.Payment;

public sealed class ConexaoMercadoPagoService(
    IConfiguration configuration, TentativasMercadoPagoStore tentativas, TimeProvider timeProvider,
    MercadoPagoOAuthClient oauthClient, IConexaoMercadoPagoRepository repository,
    IDataProtectionProvider protectionProvider) : IConexaoMercadoPagoService
{
    public IniciarConexaoMercadoPagoResponse Iniciar(string administradorId)
    {
        var clientId = configuration["MercadoPago:ClientId"];
        var clientSecret = configuration["MercadoPago:ClientSecret"];
        var redirect = configuration["MercadoPago:RedirectUri"];
        if (string.IsNullOrWhiteSpace(clientId) || !clientId.All(char.IsAsciiDigit)
            || string.IsNullOrWhiteSpace(clientSecret)
            || !Uri.TryCreate(redirect, UriKind.Absolute, out var redirectUri)
            || redirectUri.Scheme != Uri.UriSchemeHttps
            || !string.IsNullOrEmpty(redirectUri.UserInfo)
            || !string.IsNullOrEmpty(redirectUri.Fragment))
        {
            throw new MercadoPagoNaoConfiguradoException();
        }

        var state = Base64Url(RandomNumberGenerator.GetBytes(32));
        var verifier = Base64Url(RandomNumberGenerator.GetBytes(32));
        var challenge = Base64Url(SHA256.HashData(Encoding.ASCII.GetBytes(verifier)));
        var expiraEm = timeProvider.GetUtcNow().AddMinutes(10);

        // Uma nova tentativa substitui a anterior deste administrador; nada é persistido no banco.
        tentativas.Salvar(new TentativaMercadoPago(administradorId, state, verifier, expiraEm));

        var parametros = new Dictionary<string, string>
        {
            ["client_id"] = clientId,
            ["response_type"] = "code",
            ["platform_id"] = "mp",
            ["redirect_uri"] = redirect!,
            ["state"] = state,
            ["code_challenge"] = challenge,
            ["code_challenge_method"] = "S256"
        };
        var query = string.Join("&", parametros.Select(item =>
            $"{Uri.EscapeDataString(item.Key)}={Uri.EscapeDataString(item.Value)}"));
        return new IniciarConexaoMercadoPagoResponse(
            $"https://auth.mercadopago.com/authorization?{query}", expiraEm);
    }

    public async Task<ResultadoRetornoMercadoPago> ProcessarRetornoAsync(string? state, string? code, string? error,
        CancellationToken cancellationToken)
    {
        if (state is null || state.Length != 43
            || !state.All(character => char.IsAsciiLetterOrDigit(character) || character is '_' or '-')
            || (error != "access_denied" && string.IsNullOrWhiteSpace(code))
            || (error is not null && (error != "access_denied" || code is not null)))
        {
            throw new MercadoPagoCallbackInvalidoException();
        }

        var tentativa = tentativas.Consumir(state);
        if (tentativa is null)
            throw new MercadoPagoCallbackInvalidoException();
        if (error == "access_denied")
            return ResultadoRetornoMercadoPago.AutorizacaoRecusada;
        if (!bool.TryParse(configuration["MercadoPago:ConexaoEnabled"], out var enabled) || !enabled)
            return ResultadoRetornoMercadoPago.TrocaTokenPendente;

        if (!bool.TryParse(configuration["MercadoPago:Sandbox"], out var sandbox))
            throw new MercadoPagoNaoConfiguradoException();
        var tokens = await oauthClient.TrocarAsync(configuration["MercadoPago:ClientId"]!,
            configuration["MercadoPago:ClientSecret"]!, configuration["MercadoPago:RedirectUri"]!,
            code!, tentativa.Verifier, sandbox, cancellationToken);

        string protectedTokens;
        try
        {
            protectedTokens = protectionProvider.CreateProtector("BarberApp.MercadoPago.Tokens.v1")
                .Protect(JsonSerializer.Serialize(new
                {
                    access_token = tokens.AccessToken,
                    refresh_token = tokens.RefreshToken
                }));
        }
        catch (CryptographicException)
        {
            throw new MercadoPagoOperacaoException("MercadoPagoProtecaoFalhou");
        }

        var conexao = new ConexaoMercadoPago(tokens.ContaId, tentativa.AdministradorId, protectedTokens,
            timeProvider.GetUtcNow().AddSeconds(tokens.ExpiresIn), sandbox);
        try
        {
            await repository.SalvarAsync(conexao, cancellationToken);
        }
        catch (Exception exception) when (exception is DbUpdateException or NpgsqlException)
        {
            throw new MercadoPagoOperacaoException("MercadoPagoPersistenciaFalhou");
        }
        return ResultadoRetornoMercadoPago.Conectado;
    }

    private static string Base64Url(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

}
