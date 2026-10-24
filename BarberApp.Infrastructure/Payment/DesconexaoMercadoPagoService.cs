using BarberApp.Application.Services;
using BarberApp.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace BarberApp.Infrastructure.Payment;

// Remove somente a conexão local; não revoga autorização no provedor.
public sealed class DesconexaoMercadoPagoService(
    IRemocaoConexaoMercadoPagoRepository repository,
    TentativasMercadoPagoStore tentativas,
    CoordenadorOperacoesMercadoPago coordenador) : IDesconexaoMercadoPagoService
{
    public async Task DesconectarAsync(CancellationToken cancellationToken)
    {
        using var operacao = coordenador.Iniciar(cancellationToken);
        try
        {
            await repository.RemoverAsync(cancellationToken);
        }
        catch (Exception exception) when (exception is DbUpdateException or NpgsqlException)
        {
            throw new MercadoPagoOperacaoException("MercadoPagoPersistenciaFalhou");
        }

        // Mesmo sem registro persistido podem existir states capazes de reconectar a instalação.
        tentativas.InvalidarTodas();
    }
}
