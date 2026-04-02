using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BarberApp.Domain.Entities;

namespace BarberApp.Domain.Interfaces
{
    public interface IServicoRepository
    {
        Task<IEnumerable<Servico>> ObterTodosAsync();
        Task<Servico?> ObterPorIdAsync(Guid id);
        Task AdicionarAsync(Servico servico);
    }
}