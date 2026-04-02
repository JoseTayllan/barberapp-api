using BarberApp.Domain.Entities;
using BarberApp.Domain.Interfaces;

namespace BarberApp.Application.Services;

public class ServicoService
{
    private readonly IServicoRepository _repository;

    public ServicoService(IServicoRepository repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<Servico>> ListarAtivosAsync() =>
        await _repository.ObterTodosAsync();

    public async Task<Servico> CriarAsync(string nome, decimal preco, int duracaoMinutos, string? descricao)
    {
        var servico = new Servico(nome, preco, duracaoMinutos, descricao);
        await _repository.AdicionarAsync(servico);
        return servico;
    }
}