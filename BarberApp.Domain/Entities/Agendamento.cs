using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BarberApp.Domain.Enums;

namespace BarberApp.Domain.Entities
{
    public class Agendamento : BaseEntity
    {
        public Guid ClienteId { get; private set; }
        public Guid BarbeiroId { get; private set; }
        public Guid ServicoId { get; private set; }
        public DateTime DataHora { get; private set; }
        public StatusAgendamento Status { get; private set; } = StatusAgendamento.Pendente;
        public string? Observacao { get; private set; }

        //Navegação (EF Core usa isso para fazer os JOINs)
        public Cliente? Cliente { get; private set; }
        public Barbeiro? Barbeiro { get; private set; }
        public Servico? Servico { get; private set; }

        //EF Core Precisa de construtor vazio
        protected Agendamento() { }

        public Agendamento(Guid clienteId, Guid barbeiroId, Guid servicoId, DateTime dataHora, string? observacao = null)
        {
            ClienteId = clienteId;
            BarbeiroId = barbeiroId;
            ServicoId = servicoId;
            DataHora = dataHora;
            Observacao = observacao;
        }

        public void Confirmar()
        {
            if (Status == StatusAgendamento.Pendente)
                throw new InvalidOperationException("Somente agendamento pendentes podem ser confirmados.");

            Status = StatusAgendamento.Confirmado;
            AtualizadoEm = DateTime.UtcNow;
        }

        public void Cancelar()
        {
            if (Status == StatusAgendamento.Concluido)
                throw new InvalidOperationException("Agendamento concluídos não podem ser cancelados.");

            Status = StatusAgendamento.Cancelado;
            AtualizadoEm = DateTime.UtcNow;

        }
        
        public void Concluir()
        {
            if (Status != StatusAgendamento.Confirmado)
                throw new InvalidOperationException("Somente agendamento confirmados podem ser concluídos.");

            Status = StatusAgendamento.Concluido;
            AtualizadoEm = DateTime.UtcNow;
        }
    }
}