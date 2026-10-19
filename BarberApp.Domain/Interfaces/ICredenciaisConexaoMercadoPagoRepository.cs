using BarberApp.Domain.Entities;

namespace BarberApp.Domain.Interfaces;

public interface ICredenciaisConexaoMercadoPagoRepository
{
    Task<ConexaoMercadoPago?> ObterCredenciaisAsync(CancellationToken cancellationToken);
}
