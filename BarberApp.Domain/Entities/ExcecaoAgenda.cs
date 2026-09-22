using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BarberApp.Domain.Entities
{
    public class ExcecaoAgenda : BaseEntity
    {
        public Guid BarbeiroId { get; private set; }
        public DateTime Data { get; private set; } //data especifica
        public bool NaoAtende { get; private set; } = false; // true = não atende, false = atende
        public TimeSpan? HoraInicio { get; private set; } //Null não atende
        public TimeSpan? HoraFim { get; private set; } //Null não atende
        public string? Motivo { get; private set; }

        public Barbeiro? Barbeiro { get; private set; }

        public ExcecaoAgenda() { }

        public ExcecaoAgenda(Guid barbeitoId, DateTime data, bool naoAtende,
        TimeSpan? horaInicio, TimeSpan? horaFim, string? motivo)
        {
            BarbeiroId = barbeitoId;
            Data = data.Date; // Garente só data sem hora
            NaoAtende = naoAtende;
            HoraInicio = horaInicio;
            HoraFim = horaFim;
            Motivo = motivo;
        }
        
        public void Atualizar(bool naoAtende, TimeSpan? horaInicio, TimeSpan? horaFim, string? motivo)
        {
            NaoAtende = naoAtende;
            HoraInicio = horaInicio;
            HoraFim = horaFim;
            Motivo = motivo;
            AtualizadoEm = DateTime.Now;

        }
        
    }
   
}