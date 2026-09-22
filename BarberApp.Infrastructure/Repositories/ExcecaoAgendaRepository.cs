using BarberApp.Domain.Entities;
using BarberApp.Domain.Interfaces;
using BarberApp.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;


namespace BarberApp.Infrastructure.Repositories
{
    public class ExcecaoAgendaRepository : IExcecaoAgendaRepository
    {
        private readonly AppDbContext _context;

        public ExcecaoAgendaRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<ExcecaoAgenda?> ObterPorBarbeiroEDataAsync(Guid barbeiroId, DateTime data)
        {
            var dataUtc = DateTime.SpecifyKind(data, DateTimeKind.Utc);
            return await _context.ExcecoesAgenda
                .FirstOrDefaultAsync(e => e.BarbeiroId == barbeiroId && e.Data.Date == data.Date);

        }

        public async Task<IEnumerable<ExcecaoAgenda>> ObterPorBarbeiroAsync(Guid barbeiroId) =>
            await _context.ExcecoesAgenda
                .Where(e => e.BarbeiroId == barbeiroId && e.Data >= DateTime.UtcNow.Date)
                .OrderBy(e => e.Data)
                .ToListAsync();

        public async Task AdicionarAsync(ExcecaoAgenda execao)
        {
            await _context.ExcecoesAgenda.AddAsync(execao);
            await _context.SaveChangesAsync();
        }

        public async Task AtualizarAsync(ExcecaoAgenda excecao)
        {
            _context.ExcecoesAgenda.Update(excecao);
            await _context.SaveChangesAsync();
        }
        
        public async Task RemoverAsync(ExcecaoAgenda excecao)
        {
            _context.ExcecoesAgenda.Remove(excecao);
            await _context.SaveChangesAsync();
        }
    }
}