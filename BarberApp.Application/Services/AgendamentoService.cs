using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BarberApp.Application.DTOs;
using BarberApp.Domain.Entities;
using BarberApp.Domain.Interfaces;
using Microsoft.Extensions.Configuration;

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
        private readonly ExcecaoAgendaService _execaoService;

        public AgendamentoService(
            IAgendamentoRepository agendamentoRepo,
            IBarbeiroRepository barbeiroRepo,
            IServicoRepository servicoRepo,
            IClienteRepository clienteRepo,
            IConfiguration configuration,
            DisponibilidadeService disponibilidadeService,
            ExcecaoAgendaService execaoService)
        {
            _agendamentoRepo = agendamentoRepo;
            _barbeiroRepo = barbeiroRepo;
            _servicoRepo = servicoRepo;
            _clienteRepo = clienteRepo;
            _configuration = configuration;
            _disponibilidadeService = disponibilidadeService;
            _execaoService = execaoService;
        }

        public async Task<Agendamento> CriarAsync(
            string emailCliente,
            Guid barbeiroId,
            Guid servicoId,
            DateTime dataHora,
            string? observacao)
        {
            var cliente = await _clienteRepo.ObterPorEmailAsync(emailCliente)
                ?? throw new Exception("Perfil de cliente não encontrado.");

            var barbeiro = await _barbeiroRepo.ObterPorIdAsync(barbeiroId)
                ?? throw new Exception("Barbeiro não encontrado.");

            var servico = await _servicoRepo.ObterPorIdAsync(servicoId)
                ?? throw new Exception("Serviço não encontrado.");

            var fusoHorario = TimeZoneInfo.FindSystemTimeZoneById(
                "E. South America Standard Time");

            var dataHoraBrasilia = DateTime.SpecifyKind(
                dataHora,
                DateTimeKind.Unspecified);

            
    // Exceções têm prioridade sobre a agenda semanal.
    var dataUtc = DateTime.SpecifyKind(
        dataHoraBrasilia.Date,
        DateTimeKind.Utc);

    var excecao = await _execaoService.ObterPorDataAsync(
        barbeiroId,
        dataUtc);

    if (excecao is not null && excecao.NaoAtende)
        throw new Exception(
            "O barbeiro não atende neste dia.");

    TimeSpan horaInicio;
    TimeSpan horaFim;

    if (excecao is not null && !excecao.NaoAtende)
    {
        horaInicio = excecao.HoraInicio!.Value;
        horaFim = excecao.HoraFim!.Value;
    }
    else
    {
        var diaSemana =
            (Domain.Enums.DiaSemana)dataHoraBrasilia.DayOfWeek;

        var agenda =
            await _disponibilidadeService.ObterPorDiaAsync(
                barbeiroId,
                diaSemana);

        if (agenda is null)
            throw new Exception(
                "O barbeiro não atende neste dia da semana.");

        horaInicio = agenda.HoraInicio;
        horaFim = agenda.HoraFim;
    }

    var horarioAgendamento = dataHoraBrasilia.TimeOfDay;

    if (horarioAgendamento < horaInicio ||
        horarioAgendamento.Add(
            TimeSpan.FromMinutes(servico.DuracaoMinuto)) > horaFim)
    {
        throw new Exception(
            $"Horário fora do expediente. " +
            $"Atendimento das {horaInicio:hh\\:mm} " +
            $"às {horaFim:hh\\:mm}.");
    }

           

            // O banco armazena as datas em UTC.
            var dataHoraUtc = TimeZoneInfo.ConvertTimeToUtc(
                dataHoraBrasilia,
                fusoHorario);

            var agendamentosDoDia = await _agendamentoRepo
                .ObterPorBarbeiroEDataAsync(
                    barbeiroId,
                    dataHoraUtc);

            var conflito = agendamentosDoDia.Any(a =>
                a.DataHora < dataHoraUtc.AddMinutes(
                    servico.DuracaoMinuto) &&
                dataHoraUtc < a.DataHora.AddMinutes(
                    servico.DuracaoMinuto));

            if (conflito)
                throw new Exception(
                    "Já existe um agendamento neste horário para este barbeiro.");

            var agendamento = new Agendamento(
                cliente.Id,
                barbeiroId,
                servicoId,
                dataHoraUtc,
                observacao);

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

        public async Task<IEnumerable<Agendamento>> ListarPorClienteAsync(
            Guid clienteId) =>
            await _agendamentoRepo.ObterPorClienteAsync(clienteId);

        public async Task<IEnumerable<HorarioDisponivelResponse>>
            ObterHorariosDisponiveisAsync(
                Guid barbeiroId,
                Guid servicoId,
                DateTime data)
        {
            var fusoHorario = TimeZoneInfo.FindSystemTimeZoneById(
                "E. South America Standard Time");

            var agoraBrasilia = TimeZoneInfo.ConvertTimeFromUtc(
                DateTime.UtcNow,
                fusoHorario);

            var dataUtc = DateTime.SpecifyKind(
                data.Date,
                DateTimeKind.Utc);

            // Exceções têm prioridade sobre a agenda semanal.
            var excecao = await _execaoService.ObterPorDataAsync(
                barbeiroId,
                dataUtc);

            if (excecao is not null && excecao.NaoAtende)
                throw new Exception(
                    "O barbeiro não atende neste dia.");

            TimeSpan abertura;
            TimeSpan fechamento;

            if (excecao is not null && !excecao.NaoAtende)
            {
                abertura = excecao.HoraInicio!.Value;
                fechamento = excecao.HoraFim!.Value;
            }
            else
            {
                var diaSemana = (Domain.Enums.DiaSemana)data.DayOfWeek;
                var agenda =await _disponibilidadeService.ObterPorDiaAsync(barbeiroId, diaSemana);

                if (agenda is null)
                    throw new Exception(
                        "O barbeiro não atende neste dia da semana.");

                abertura = agenda.HoraInicio;
                fechamento = agenda.HoraFim;
            }

            var barbeiro = await _barbeiroRepo.ObterPorIdAsync(barbeiroId)
                ?? throw new Exception("Barbeiro não encontrado.");

            var servico = await _servicoRepo.ObterPorIdAsync(servicoId)
                ?? throw new Exception("Serviço não encontrado.");

            var agendamentosDoDia =
                await _agendamentoRepo.ObterPorBarbeiroEDataAsync(
                    barbeiroId,
                    dataUtc);

            var agendamentosAtivos = agendamentosDoDia
                .Where(a =>
                    a.Status == Domain.Enums.StatusAgendamento.Pendente ||
                    a.Status == Domain.Enums.StatusAgendamento.Confirmado)
                .ToList();

            var slots = new List<HorarioDisponivelResponse>();

            var slotAtual = data.Date.Add(abertura);
            var fim = data.Date.Add(fechamento);

            while (slotAtual.AddMinutes(servico.DuracaoMinuto) <= fim)
            {
                // A comparação com os agendamentos é feita em UTC.
                var slotUtc = TimeZoneInfo.ConvertTimeToUtc(
                    DateTime.SpecifyKind(
                        slotAtual,
                        DateTimeKind.Unspecified),
                    fusoHorario);

                var ocupado = agendamentosAtivos.Any(a =>
                {
                    var inicioAgendamento = a.DataHora;
                    var fimAgendamento = a.DataHora.AddMinutes(
                        servico.DuracaoMinuto);

                    var fimSlotUtc = slotUtc.AddMinutes(
                        servico.DuracaoMinuto);

                    return slotUtc < fimAgendamento &&
                           fimSlotUtc > inicioAgendamento;
                });

                var disponivel =
                    !ocupado &&
                    slotAtual > agoraBrasilia;

                slots.Add(
                    new HorarioDisponivelResponse(
                        slotAtual.ToString("HH:mm"),
                        disponivel));

                slotAtual = slotAtual.AddMinutes(
                    servico.DuracaoMinuto);
            }

            return slots;
        }

        public async Task<IEnumerable<Agendamento>>
            ListarPorBarbeiroAsync(Guid barbeiroId) =>
            await _agendamentoRepo.ObterPorBarbeiroAsync(barbeiroId);
    }
}