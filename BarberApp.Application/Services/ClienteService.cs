using BarberApp.Domain.Entities;
using BarberApp.Domain.Interfaces;

namespace BarberApp.Application.Services;

public class ClienteService
{
    private readonly IClienteRepository _repository;

    public ClienteService(IClienteRepository repository)
    {
        _repository = repository;
    }

    public async Task<Cliente> CriarAsync(string nome, string email, string telefone)
    {
        var existente = await _repository.ObterPorEmailAsync(email);
        if (existente is not null)
            throw new Exception("Já existe um cliente com este e-mail.");

        var cliente = new Cliente(nome, email, telefone);
        await _repository.AdicionarAsync(cliente);
        return cliente;
    }

    public async Task<Cliente?> ObterPorIdAsync(Guid id) =>
        await _repository.ObterPorIdAsync(id);
}