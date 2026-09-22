using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using BarberApp.Domain.Entities;

namespace BarberApp.Domain.Interfaces
{
    public interface IExcecaoAgendaRepository
    {
        Task<ExcecaoAgenda?> ObterPorBarbeiroEDataAsync(Guid barbeiroId, DateTime data);
        Task<IEnumerable<ExcecaoAgenda>> ObterPorBarbeiroAsync(Guid barbeiroId);
        Task AdicionarAsync(ExcecaoAgenda excecao);
        Task AtualizarAsync(ExcecaoAgenda excecao);
        Task RemoverAsync(ExcecaoAgenda excecao);
    }
}