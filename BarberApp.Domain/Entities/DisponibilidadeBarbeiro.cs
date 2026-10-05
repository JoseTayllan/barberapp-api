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
            if (!Enum.IsDefined(diaSemana))
                throw new ArgumentException("Dia da semana inválido.", nameof(diaSemana));

            ValidarIntervalo(horaInicio, horaFim);
            BarbeiroId = barbeiroId;
            DiaSemana = diaSemana;
            HoraInicio = horaInicio;
            HoraFim = horaFim;
        }

        public void Atualizar(TimeSpan horaInicio, TimeSpan horaFim, bool ativo)
        {
            ValidarIntervalo(horaInicio, horaFim);
            HoraInicio = horaInicio;
            HoraFim = horaFim;
            Ativo = ativo;
            AtualizadoEm  = DateTime.UtcNow;
        }

        private static void ValidarIntervalo(TimeSpan horaInicio, TimeSpan horaFim)
        {
            if (horaInicio < TimeSpan.Zero || horaInicio >= horaFim || horaFim > TimeSpan.FromDays(1))
                throw new ArgumentException("O intervalo deve estar dentro do dia, com início anterior ao fim.");
        }
        
    }
}
