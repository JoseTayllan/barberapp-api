using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using BarberApp.Domain.Entities;

namespace BarberApp.Domain.Interfaces
{
    public interface IAgendamentoRepository
    { 
        Task<IEnumerable<Agendamento>> ObterTodosAsync();
        Task<Agendamento?> ObterPorIdAsync(Guid id);
        Task<IEnumerable<Agendamento>> ObterPorBarbeiroEDataAsync(Guid barbeiroId, DateTime data);
        Task AdicionarAsync(Agendamento agendamento);
        Task AtualizarAsync(Agendamento agendamento);

        Task<IEnumerable<Agendamento>> ObterPorClienteAsync(Guid clienteId);
        Task<IEnumerable<Agendamento>> ObterPorBarbeiroAsync(Guid barbeiroId);

    }
}