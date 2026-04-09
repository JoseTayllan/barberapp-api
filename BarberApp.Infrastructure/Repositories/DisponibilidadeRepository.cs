using BarberApp.Domain.Entities;
using BarberApp.Domain.Enums;
using BarberApp.Domain.Interfaces;
using BarberApp.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BarberApp.Infrastructure.Repositories;

public class DisponibilidadeRepository : IDisponibilidadeRepository
{
    private readonly AppDbContext _context;

    public DisponibilidadeRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<DisponibilidadeBarbeiro>> ObterPorBarbeiroAsync(Guid barbeiroId) =>
        await _context.DisponibilidadesBarbeiros
            .Where(d => d.BarbeiroId == barbeiroId && d.Ativo)
            .OrderBy(d => d.DiaSemana)
            .ToListAsync();

    public async Task<DisponibilidadeBarbeiro?> ObterPorBarbeiroEDiaAsync(Guid barbeiroId, DiaSemana dia) =>
        await _context.DisponibilidadesBarbeiros
            .FirstOrDefaultAsync(d => d.BarbeiroId == barbeiroId &&
                                      d.DiaSemana == dia &&
                                      d.Ativo);

    public async Task AdicionarAsync(DisponibilidadeBarbeiro disponibilidade)
    {
        await _context.DisponibilidadesBarbeiros.AddAsync(disponibilidade);
        await _context.SaveChangesAsync();
    }

    public async Task AtualizarAsync(DisponibilidadeBarbeiro disponibilidade)
    {
        _context.DisponibilidadesBarbeiros.Update(disponibilidade);
        await _context.SaveChangesAsync();
    }
}