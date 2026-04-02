using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BarberApp.Domain.Entities;
using BarberApp.Domain.Interfaces;

namespace BarberApp.Application.Services
{
    public class BarbeiroService
    {
        private readonly IBarbeiroRepository _repository;

        public BarbeiroService(IBarbeiroRepository repository)
        {
            _repository = repository;
        }

        public async Task<IEnumerable<Barbeiro>> ListarAtivosAsync() =>
            await _repository.ObterTodosAsync();

        public async Task<Barbeiro> CriarAsync(string nome, string telefone, string? foto = null)
        {
            var barbeiro = new Barbeiro(nome, telefone, foto);
            await _repository.AdicionarAsync(barbeiro);
            return barbeiro;
        }

        public async Task<Barbeiro?> ObterPorIdAsync(Guid id) =>
            await _repository.ObterPorIdAsync(id);
    }
}