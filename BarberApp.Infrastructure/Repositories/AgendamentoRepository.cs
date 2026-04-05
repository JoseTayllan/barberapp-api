using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using BarberApp.Domain.Entities;
using BarberApp.Domain.Interfaces;
using BarberApp.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BarberApp.Infrastructure.Repositories
{
    public class AgendamentoRepository : IAgendamentoRepository
    {
        private readonly AppDbContext _context;

        public AgendamentoRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Agendamento>> ObterTodosAsync() =>
            await _context.Agendamentos
                .Include(a => a.Cliente)
                .Include(a => a.Barbeiro)
                .Include(a => a.Servico)
                .OrderBy(a => a.DataHora)
                .ToListAsync();

        public async Task<Agendamento?> ObterPorIdAsync(Guid id) =>
            await _context.Agendamentos
                .Include(a => a.Cliente)
                .Include(a => a.Barbeiro)
                .Include(a => a.Servico)
                .FirstOrDefaultAsync(a => a.Id == id);
        public async Task<IEnumerable<Agendamento>> ObterPorBarbeiroEDataAsync(Guid barbeiroId, DateTime data) =>
        await _context.Agendamentos
            .Where(a => a.BarbeiroId == barbeiroId && a.DataHora.Date == data.Date)
            .OrderBy(a => a.DataHora)
            .ToListAsync();

        public async Task AdicionarAsync(Agendamento agendamento)
        {
            await _context.Agendamentos.AddAsync(agendamento);
            await _context.SaveChangesAsync();
        }

        public async Task AtualizarAsync(Agendamento agendamento)
        {
            _context.Agendamentos.Update(agendamento);
            await _context.SaveChangesAsync();
        }
        public async Task<IEnumerable<Agendamento>> ObterPorClienteAsync(Guid clienteId) =>
        await _context.Agendamentos
           .Include(a => a.Cliente)
           .Include(a => a.Barbeiro)
           .Include(a => a.Servico)
           .Where(a => a.ClienteId == clienteId)
           .OrderBy(a => a.DataHora)
           .ToListAsync();
    }
}