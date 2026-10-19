using BarberApp.Application.Services;

namespace BarberApp.Infrastructure.Payment;

// Compartilhado apenas nesta instância; não é coordenação distribuída.
public sealed class CoordenadorOperacoesMercadoPago
{
    private int _emAndamento;

    internal IDisposable Iniciar(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (Interlocked.CompareExchange(ref _emAndamento, 1, 0) != 0)
            throw new MercadoPagoOperacaoException("MercadoPagoOperacaoEmAndamento");
        return new Escopo(this);
    }

    private sealed class Escopo(CoordenadorOperacoesMercadoPago coordenador) : IDisposable
    {
        private CoordenadorOperacoesMercadoPago? _coordenador = coordenador;

        public void Dispose()
        {
            var atual = Interlocked.Exchange(ref _coordenador, null);
            if (atual is not null) Volatile.Write(ref atual._emAndamento, 0);
        }
    }
}
