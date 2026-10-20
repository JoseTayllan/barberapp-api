namespace BarberApp.Domain.Interfaces;

public interface IPendenciaConexaoMercadoPagoRepository
{
    Task<bool> RegistrarPendenciaAsync(CancellationToken cancellationToken);
}
