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
    public class BarbeiroRepository : IBarbeiroRepository
    {
        private readonly AppDbContext _context;

        public BarbeiroRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Barbeiro>> ObterTodosAsync() =>
            await _context.Barbeiros
               .Where(b => b.Ativo) // Filtra apenas barbeiros ativos
               .OrderBy(b => b.Nome)
               .ToListAsync();

        public async Task<Barbeiro?> ObterPorIdAsync(Guid id) =>
            await _context.Barbeiros.FindAsync(id);

        public async Task AdicionarAsync(Barbeiro barbeiro)
        {
            await _context.Barbeiros.AddAsync(barbeiro);
            await _context.SaveChangesAsync();
        }

        public async Task AtualizarAsync(Barbeiro barbeiro)
        {
            _context.Barbeiros.Update(barbeiro);
            await _context.SaveChangesAsync();
        }

    }
}