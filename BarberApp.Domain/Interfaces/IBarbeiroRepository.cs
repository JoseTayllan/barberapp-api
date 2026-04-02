using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BarberApp.Domain.Entities;

namespace BarberApp.Domain.Interfaces
{
    public interface IBarbeiroRepository
    {
        Task<IEnumerable<Barbeiro>> ObterTodosAsync();
        Task<Barbeiro?> ObterPorIdAsync(Guid id);
        Task AdicionarAsync(Barbeiro barbeiro);
        Task AtualizarAsync(Barbeiro barbeiro);
    }
}