using BarberApp.Domain.Entities;
using BarberApp.Domain.Interfaces;
using BarberApp.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BarberApp.Infrastructure.Repositories;

public class ClienteRepository : IClienteRepository
{
    private readonly AppDbContext _context;

    public ClienteRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Cliente?> ObterPorIdAsync(Guid id) =>
        await _context.Clientes.FirstOrDefaultAsync(c => c.Id == id);

    public async Task<Cliente?> ObterPorEmailAsync(string email) =>
        await _context.Clientes.FirstOrDefaultAsync(c => c.Email == email);

    public async Task AdicionarAsync(Cliente cliente)
    {
        await _context.Clientes.AddAsync(cliente);
        await _context.SaveChangesAsync();
    }
}