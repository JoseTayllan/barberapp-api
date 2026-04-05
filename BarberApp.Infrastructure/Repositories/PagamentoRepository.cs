
using BarberApp.Domain.Entities;
using BarberApp.Domain.Interfaces;
using BarberApp.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BarberApp.Infrastructure.Repositories
{
    public class PagamentoRepository : IPagamentoRepository
    {
        private readonly AppDbContext _context;
        public PagamentoRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Pagamento?> ObterPorAgendamentoAsync(Guid agendamentoId) =>
          await _context.Pagamentos
            .FirstOrDefaultAsync(p => p.AgendamentoId == agendamentoId);

        public async Task AdicionarAsync(Pagamento pagamento)
        {
            await _context.Pagamentos.AddAsync(pagamento);
            await _context.SaveChangesAsync();
        }
        
        public async Task AtualizarAsync(Pagamento pagamento)
        {
            _context.Pagamentos.Update(pagamento);
            await _context.SaveChangesAsync();
        }

    }
}