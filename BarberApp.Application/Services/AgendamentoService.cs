using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BarberApp.Application.DTOs;
using BarberApp.Domain.Entities;
using BarberApp.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualBasic;
namespace BarberApp.Application.Services
{
    public class AgendamentoService
    {
        private readonly IAgendamentoRepository _agendamentoRepo;
        private readonly IBarbeiroRepository _barbeiroRepo;
        private readonly IServicoRepository _servicoRepo;
        private readonly IClienteRepository _clienteRepo;
        private readonly IConfiguration _configuration;
        private readonly DisponibilidadeService _disponibilidadeService;

        public AgendamentoService(
            IAgendamentoRepository agendamentoRepo,
            IBarbeiroRepository barbeiroRepo,
            IServicoRepository servicoRepo,
            IClienteRepository clienteRepo,
            IConfiguration configuration,
            DisponibilidadeService disponibilidadeService)
        {
            _agendamentoRepo = agendamentoRepo;
            _barbeiroRepo = barbeiroRepo;
            _servicoRepo = servicoRepo;
            _clienteRepo = clienteRepo;
            _configuration = configuration;
            _disponibilidadeService = disponibilidadeService;
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

        public async Task<IEnumerable<HorarioDisponivelResponse>> ObterHorariosDisponiveisAsync(
     Guid barbeiroId, Guid servicoId, DateTime data)
        {
            // Fuso horário de Brasília
            var fusoHorario = TimeZoneInfo.FindSystemTimeZoneById("E. South America Standard Time");
            var agoraBrasilia = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, fusoHorario);

            var diaSemana = (Domain.Enums.DiaSemana)data.DayOfWeek;
            var agenda = await _disponibilidadeService.ObterPorDiaAsync(barbeiroId, diaSemana);

            if (agenda is null)
                throw new Exception($"O barbeiro não atende neste dia da semana.");

            var abertura = agenda.HoraInicio;
            var fechamento = agenda.HoraFim;

            var barbeiro = await _barbeiroRepo.ObterPorIdAsync(barbeiroId)
                ?? throw new Exception("Barbeiro não encontrado.");

            var servico = await _servicoRepo.ObterPorIdAsync(servicoId)
                ?? throw new Exception("Serviço não encontrado.");

            // Busca agendamentos usando UTC
            var dataUtc = DateTime.SpecifyKind(data.Date, DateTimeKind.Utc);
            var agendamentosDoDia = await _agendamentoRepo
                .ObterPorBarbeiroEDataAsync(barbeiroId, dataUtc);

            var agendamentosAtivos = agendamentosDoDia
                .Where(a => a.Status == Domain.Enums.StatusAgendamento.Pendente ||
                            a.Status == Domain.Enums.StatusAgendamento.Confirmado)
                .ToList();

            var slots = new List<HorarioDisponivelResponse>();

            // Gera slots no horário de Brasília
            var slotAtual = data.Date.Add(abertura);
            var fim = data.Date.Add(fechamento);

            // DEBUG TEMPORÁRIO
            // Console.WriteLine($"Agora Brasília: {agoraBrasilia}");
            // Console.WriteLine($"Primeiro slot: {slotAtual}");
            // Console.WriteLine($"Data recebida: {data}");

            while (slotAtual.AddMinutes(servico.DuracaoMinuto) <= fim)
            {
                // Converte o slot para UTC para comparar com os agendamentos do banco
                var slotUtc = TimeZoneInfo.ConvertTimeToUtc(
                    DateTime.SpecifyKind(slotAtual, DateTimeKind.Unspecified), fusoHorario);

                var ocupado = agendamentosAtivos.Any(a =>
                {
                    var inicioAgendamento = a.DataHora;
                    var fimAgendamento = a.DataHora.AddMinutes(servico.DuracaoMinuto);
                    var fimSlotUtc = slotUtc.AddMinutes(servico.DuracaoMinuto);
                    return slotUtc < fimAgendamento && fimSlotUtc > inicioAgendamento;
                });

                // Compara com horário atual de Brasília
                var disponivel = !ocupado && slotAtual > agoraBrasilia;

                slots.Add(new HorarioDisponivelResponse(
                    slotAtual.ToString("HH:mm"),
                    disponivel
                ));

                slotAtual = slotAtual.AddMinutes(servico.DuracaoMinuto);
            }

            return slots;
        }
    }
}