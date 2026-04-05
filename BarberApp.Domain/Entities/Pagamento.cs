using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BarberApp.Domain.Enums;

namespace BarberApp.Domain.Entities
{
    public class Pagamento : BaseEntity
    {
        public Guid AgendamentoId { get; private set; }
        public decimal Valor { get; private set; }
        public StatusPagemento Status { get; private set; } = StatusPagemento.Pendente;
        public string? GatewayTransacaoId { get; private set; } //! ID retornado pelo MercadoPago/Stripe
        public string? Gateway { get; private set; } //todo "MercadoPago", "Stripe", etc.

        public Agendamento? Agendamento { get; private set; }

        protected Pagamento() { }

        public Pagamento(Guid agendamentoId, decimal valor)
        {
            AgendamentoId = agendamentoId;
            Valor = valor;
        }

        public void Aprovar(string gatewayTransacaoId, string gateway)
        {
            Status = StatusPagemento.Aprovado;
            GatewayTransacaoId = gatewayTransacaoId;
            Gateway = gateway;
            AtualizadoEm = DateTime.UtcNow;
        }

        public void Recusar()
        {
            Status = StatusPagemento.Recusado;
            AtualizadoEm = DateTime.UtcNow;
        }

        public void Reenbolsar()
        {
            if (Status != StatusPagemento.Aprovado)
                throw new InvalidOperationException("Somente pagamentos aprovados podem ser reenbolsados.");

            Status = StatusPagemento.Reenbolsado;
            AtualizadoEm = DateTime.UtcNow;
        }

    }
}