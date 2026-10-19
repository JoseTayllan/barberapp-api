using System.Security.Cryptography;
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

public sealed class RenovacaoMercadoPagoService(
    IConfiguration configuration, ICredenciaisConexaoMercadoPagoRepository credenciaisRepository,
    IConexaoMercadoPagoRepository repository, IDataProtectionProvider protectionProvider,
    MercadoPagoOAuthClient oauthClient, TimeProvider timeProvider,
    CoordenadorOperacoesMercadoPago coordenador) : IRenovacaoMercadoPagoService
{
    public async Task<StatusConexaoMercadoPagoResponse> RenovarAsync(CancellationToken cancellationToken)
    {
        if (!bool.TryParse(configuration["MercadoPago:ConexaoEnabled"], out var enabled) || !enabled)
            throw new MercadoPagoOperacaoException("MercadoPagoConexaoDesabilitada");

        var clientId = configuration["MercadoPago:ClientId"];
        var clientSecret = configuration["MercadoPago:ClientSecret"];
        if (string.IsNullOrWhiteSpace(clientId) || !clientId.All(char.IsAsciiDigit)
            || string.IsNullOrWhiteSpace(clientSecret)
            || !bool.TryParse(configuration["MercadoPago:Sandbox"], out var sandbox))
            throw new MercadoPagoNaoConfiguradoException();

        // Ler as credenciais somente após admissão evita usar um snapshot anterior à reconexão.
        using var operacao = coordenador.Iniciar(cancellationToken);
        ConexaoMercadoPago? conexao;
        try
        {
            conexao = await credenciaisRepository.ObterCredenciaisAsync(cancellationToken);
        }
        catch (NpgsqlException)
        {
            throw new MercadoPagoOperacaoException("MercadoPagoConsultaFalhou");
        }
        if (conexao is null)
            throw new MercadoPagoOperacaoException("MercadoPagoNaoConectado");
        if (conexao.Sandbox != sandbox)
            throw new MercadoPagoOperacaoException("MercadoPagoAmbienteIncompativel");

        var protector = protectionProvider.CreateProtector("BarberApp.MercadoPago.Tokens.v1");
        string? refreshToken;
        try
        {
            using var payload = JsonDocument.Parse(protector.Unprotect(conexao.TokensProtegidos));
            refreshToken = payload.RootElement.GetProperty("refresh_token").GetString();
            if (string.IsNullOrWhiteSpace(refreshToken))
                throw new MercadoPagoOperacaoException("MercadoPagoCredenciaisIndisponiveis");
        }
        catch (Exception exception) when (exception is CryptographicException or JsonException
            or KeyNotFoundException or InvalidOperationException)
        {
            throw new MercadoPagoOperacaoException("MercadoPagoCredenciaisIndisponiveis");
        }

        var tokens = await oauthClient.RenovarAsync(clientId, clientSecret, refreshToken, sandbox, cancellationToken);
        if (tokens.ContaId != conexao.ContaId)
            throw new MercadoPagoOperacaoException("MercadoPagoRespostaInvalida");

        string protectedTokens;
        try
        {
            protectedTokens = protector.Protect(JsonSerializer.Serialize(new
            {
                access_token = tokens.AccessToken,
                refresh_token = tokens.RefreshToken
            }));
        }
        catch (CryptographicException)
        {
            throw new MercadoPagoOperacaoException("MercadoPagoProtecaoFalhou");
        }

        var expiraEm = timeProvider.GetUtcNow().AddSeconds(tokens.ExpiresIn);
        var renovada = new ConexaoMercadoPago(conexao.ContaId, conexao.AdministradorId,
            protectedTokens, expiraEm, conexao.Sandbox);
        try
        {
            await repository.SalvarAsync(renovada, cancellationToken);
        }
        catch (Exception exception) when (exception is DbUpdateException or NpgsqlException)
        {
            throw new MercadoPagoOperacaoException("MercadoPagoPersistenciaFalhou");
        }
        return new StatusConexaoMercadoPagoResponse("Conectado", conexao.ContaId, expiraEm, conexao.Sandbox);
    }
}
