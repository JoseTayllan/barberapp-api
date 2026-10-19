using BarberApp.Application.DTOs;

namespace BarberApp.Application.Services;

public interface IRenovacaoMercadoPagoService
{
    Task<StatusConexaoMercadoPagoResponse> RenovarAsync(CancellationToken cancellationToken);
}
