using BarberApp.Domain.Entities;
using BarberApp.Domain.Enums;

namespace BarberApp.Domain.Interfaces;

public interface IDisponibilidadeRepository
{
    Task<IEnumerable<DisponibilidadeBarbeiro>> ObterPorBarbeiroAsync(Guid barbeiroId);
    Task<DisponibilidadeBarbeiro?> ObterPorBarbeiroEDiaAsync(Guid barbeiroId, DiaSemana dia);
    Task AdicionarAsync(DisponibilidadeBarbeiro disponibilidade);
    Task AtualizarAsync(DisponibilidadeBarbeiro disponibilidade);
}