using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading.Tasks;

using BarberApp.Domain.Entities;
using BarberApp.Domain.Interfaces;
using BarberApp.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BarberApp.Infrastructure.Repositories
{
    public class ServicoRepository : IServicoRepository
    {
        private readonly AppDbContext _context;

        public ServicoRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Servico>> ObterTodosAsync() =>
            await _context.Servicos
                .Where(s => s.Ativo)
                .OrderBy(s => s.Nome)
                .ToListAsync();

        public async Task<Servico?> ObterPorIdAsync(Guid id) =>
            await _context.Servicos.FindAsync(id);
        
        
        public async Task AdicionarAsync(Servico servico)
        {
            await _context.Servicos.AddAsync(servico);
            await _context.SaveChangesAsync();
        }
    }
}