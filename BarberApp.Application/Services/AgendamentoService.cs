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

        public AgendamentoService(
            IAgendamentoRepository agendamentoRepo,
            IBarbeiroRepository barbeiroRepo,
            IServicoRepository servicoRepo)
        {
            _agendamentoRepo = agendamentoRepo;
            _barbeiroRepo = barbeiroRepo;
            _servicoRepo = servicoRepo;
        }

        public async Task<Agendamento> CriarAsync(Guid clienteId, Guid barbeiroId, Guid servicoId, DateTime dateHora, string? observacao = null)
        {
            // Valida se barbeiro existe
            var barbeiro = await _barbeiroRepo.ObterPorIdAsync(barbeiroId)
                ?? throw new Exception("Barbeiro não encontrado");

            // Valida se serviço existe
            var servico = await _servicoRepo.ObterPorIdAsync(servicoId)
                ?? throw new Exception("Serviço não encontrado");

            // Verifica conflito de horário
            var agendamentosDoDia = await _agendamentoRepo
            .ObterPorBarbeiroEDataAsync(barbeiroId, dateHora.Date);

            var conflito = agendamentosDoDia.Any(a =>
            a.DataHora < dateHora.AddMinutes(servico.DuracaoMinuto) &&
            dateHora < a.DataHora.AddMinutes(servico.DuracaoMinuto)
            );

            if (conflito)
                throw new Exception("Já existe um agendamento neste horário para este barbeiro.");

            var agendamento = new Agendamento(clienteId, barbeiroId, servicoId, dateHora, observacao);
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

    }
}