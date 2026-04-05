using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BarberApp.Domain.Entities;
using BarberApp.Domain.Interfaces;

namespace BarberApp.Application.Services
{
    public class AgendamentoService
    {
        private readonly IAgendamentoRepository _agendamentoRepo;
        private readonly IBarbeiroRepository _barbeiroRepo;
        private readonly IServicoRepository _servicoRepo;
        private readonly IClienteRepository _clienteRepo;

        public AgendamentoService(
            IAgendamentoRepository agendamentoRepo,
            IBarbeiroRepository barbeiroRepo,
            IServicoRepository servicoRepo,
            IClienteRepository clienteRepo)
        {
            _agendamentoRepo = agendamentoRepo;
            _barbeiroRepo = barbeiroRepo;
            _servicoRepo = servicoRepo;
            _clienteRepo = clienteRepo;
        }

        public async Task<Agendamento> CriarAsync(string emailCliente, Guid barbeiroId, Guid servicoId, DateTime dataHora, string? observacao)
        {
            // Resolve o cliente pelo email do token
            var cliente = await _clienteRepo.ObterPorEmailAsync(emailCliente)
                ?? throw new Exception("Perfil de cliente não encontrado.");

            var barbeiro = await _barbeiroRepo.ObterPorIdAsync(barbeiroId)
                ?? throw new Exception("Barbeiro não encontrado.");

            var servico = await _servicoRepo.ObterPorIdAsync(servicoId)
                ?? throw new Exception("Serviço não encontrado.");

            var agendamentosDoDia = await _agendamentoRepo
                .ObterPorBarbeiroEDataAsync(barbeiroId, dataHora);

            var conflito = agendamentosDoDia.Any(a =>
                a.DataHora < dataHora.AddMinutes(servico.DuracaoMinuto) &&
                dataHora < a.DataHora.AddMinutes(servico.DuracaoMinuto));

            if (conflito)
                throw new Exception("Já existe um agendamento neste horário para este barbeiro.");

            var agendamento = new Agendamento(cliente.Id, barbeiroId, servicoId, dataHora, observacao);
            await _agendamentoRepo.AdicionarAsync(agendamento);
            return agendamento;
        }

        public async Task<Agendamento?> ObterPorIdAsync(Guid id) =>
            await _agendamentoRepo.ObterPorIdAsync(id);

        public async Task<IEnumerable<Agendamento>> ListarTodosAsync() =>
            await _agendamentoRepo.ObterTodosAsync();

        public async Task ConfirmarAsync(Guid id)
        {
            var agendamento = await _agendamentoRepo.ObterPorIdAsync(id)
                ?? throw new Exception("Agendamento não encontrado.");

            agendamento.Confirmar();
            await _agendamentoRepo.AtualizarAsync(agendamento);
        }

        public async Task CancelarAsync(Guid id)
        {
            var agendamento = await _agendamentoRepo.ObterPorIdAsync(id)
                ?? throw new Exception("Agendamento não encontrado.");

            agendamento.Cancelar();
            await _agendamentoRepo.AtualizarAsync(agendamento);
        }

        public async Task<IEnumerable<Agendamento>> ListarPorClienteAsync(Guid clienteId) =>
            await _agendamentoRepo.ObterPorClienteAsync(clienteId);

    }
}