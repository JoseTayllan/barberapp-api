using BarberApp.Domain.Entities;

namespace BarberApp.Domain.Interfaces;

public interface IConsultaConexaoMercadoPagoRepository
{
    Task<MetadadosConexaoMercadoPago?> ConsultarAsync(CancellationToken cancellationToken);
}
