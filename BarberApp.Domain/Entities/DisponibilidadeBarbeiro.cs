using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using BarberApp.Domain.Enums;

namespace BarberApp.Domain.Entities
{
    public class DisponibilidadeBarbeiro : BaseEntity
    {
        public Guid BarbeiroId { get; private set; }
        public DiaSemana DiaSemana { get; private set; }
        public TimeSpan HoraInicio { get; private set; }
        public TimeSpan HoraFim { get; private set; }
        public bool Ativo { get; private set; } = true;

        public Barbeiro? Barbeiro { get; private set; }

        protected DisponibilidadeBarbeiro() { }

        public DisponibilidadeBarbeiro(Guid barbeiroId, DiaSemana diaSemana, TimeSpan horaInicio, TimeSpan horaFim)
        {
            BarbeiroId = barbeiroId;
            DiaSemana = diaSemana;
            HoraInicio = horaInicio;
            HoraFim = horaFim;
        }

        public void Atualizar(TimeSpan horaInicio, TimeSpan horaFim, bool ativo)
        {
            HoraInicio = horaInicio;
            HoraFim = horaFim;
            Ativo = ativo;
            AtualizadoEm  = DateTime.UtcNow;
        }
        
    }
}