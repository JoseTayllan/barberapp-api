namespace BarberApp.Application.Services;

public interface IDesconexaoMercadoPagoService
{
    Task DesconectarAsync(CancellationToken cancellationToken);
}
