using BarberApp.Application.DTOs;

namespace BarberApp.Application.Services;

public interface IConexaoMercadoPagoService
{
    IniciarConexaoMercadoPagoResponse Iniciar(string administradorId);
    Task<StatusConexaoMercadoPagoResponse> ConsultarStatusAsync(CancellationToken cancellationToken);
    Task<ResultadoRetornoMercadoPago> ProcessarRetornoAsync(string? state, string? code, string? error,
        CancellationToken cancellationToken);
}
