using BarberApp.Domain.Entities;

namespace BarberApp.Domain.Interfaces;

public interface IPagamentoRepository
{
    Task<Pagamento?> ObterPorAgendamentoAsync(Guid agendamentoId);
    Task AdicionarAsync(Pagamento pagamento);
    Task AtualizarAsync(Pagamento pagamento);
}