using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using BarberApp.Domain.Entities;
using BarberApp.Domain.Interfaces;

namespace BarberApp.Application.Services
{
    public class PagamentoService
    {
        private readonly IPagamentoRepository _pagamentoRepo;
        private readonly IAgendamentoRepository _agendamentoRepo;
        private readonly IPaymentService _paymentService;

        public PagamentoService(
            IPagamentoRepository pagamentoRepo,
            IAgendamentoRepository agendamentoRepo,
            IPaymentService paymentService)
        {
            _pagamentoRepo = pagamentoRepo;
            _agendamentoRepo = agendamentoRepo;
            _paymentService = paymentService;
        }

        public async Task<Pagamento> ProcessarAsync(Guid agendamentoId, string emailCliente)
        {
            var agendamento = await _agendamentoRepo.ObterPorIdAsync(agendamentoId)
                ?? throw new Exception("Agendamento não encontrado.");

            if (agendamento.Servico is null)
                throw new Exception("Serviço do agendamento não encontrado.");

            var pagamentoExistente = await _pagamentoRepo.ObterPorAgendamentoAsync(agendamentoId);
            if (pagamentoExistente?.Status == Domain.Enums.StatusPagemento.Aprovado)
                throw new Exception("Pagamento já processado para este agendamento.");

            var pagamento = new Pagamento(agendamentoId, agendamento.Servico.Preco);
            await _pagamentoRepo.AdicionarAsync(pagamento);

            var request = new PaymentRequest(
                agendamentoId,
                agendamento.Servico.Preco,
                agendamento.Servico.Nome,
                emailCliente
                );

            var resultado = await _paymentService.ProcessarPagamentoAsync(request);

            if (resultado.Sucesso)
            {
                pagamento.Aprovar(resultado.TransacaoId!, "Mock"); //todo: gateway dinâmico
                // agendamento.Confirmar(); // Confirma o agendamento somente se o pagamento for aprovado
                // await _agendamentoRepo.AtualizarAsync(agendamento);
            }
            else
                pagamento.Recusar();

            await _pagamentoRepo.AtualizarAsync(pagamento);
            return pagamento;
        }
    }
}