using BarberApp.Domain.Entities;

namespace BarberApp.Domain.Interfaces;

public interface IConexaoMercadoPagoRepository
{
    Task SalvarAsync(ConexaoMercadoPago conexao, CancellationToken cancellationToken);
}
