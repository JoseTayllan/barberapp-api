namespace BarberApp.Domain.Interfaces;

public interface IRemocaoConexaoMercadoPagoRepository
{
    Task RemoverAsync(CancellationToken cancellationToken);
}
