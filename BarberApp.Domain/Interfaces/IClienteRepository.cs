using BarberApp.Domain.Entities;

namespace BarberApp.Domain.Interfaces;

public interface IClienteRepository
{
    Task<Cliente?> ObterPorIdAsync(Guid id);
    Task<Cliente?> ObterPorEmailAsync(string email);
    Task AdicionarAsync(Cliente cliente);
    Task AtualizarAsync(Cliente cliente);
}