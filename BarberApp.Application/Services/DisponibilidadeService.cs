
using BarberApp.Domain.Entities;
using BarberApp.Domain.Enums;
using BarberApp.Domain.Interfaces;

namespace BarberApp.Application.Services
{
    public class DisponibilidadeService
    {
        private readonly IDisponibilidadeRepository _repository;
        private readonly IBarbeiroRepository _barbeiroRepository;

        public DisponibilidadeService(
            IDisponibilidadeRepository repository,
            IBarbeiroRepository barbeiroRepository)
        {
            _repository = repository;
            _barbeiroRepository = barbeiroRepository;
        }
        public async Task<IEnumerable<DisponibilidadeBarbeiro>> ListarPorBarbeiroAsync(Guid barbeiroId) =>
        await _repository.ObterPorBarbeiroAsync(barbeiroId);

        public async Task<DisponibilidadeBarbeiro> CriarAsync(
            Guid barbeiroId,
            string diaSemana,
            string horarioInicio,
            string horarioFim)
        {
            var barbeiro = await _barbeiroRepository.ObterPorIdAsync(barbeiroId)
            ?? throw new Exception("Barbeiro não encontrado");

            if (!Enum.TryParse<DiaSemana>(diaSemana, true, out var dia))
                throw new Exception($"Dia da semana inválido. Use: {string.Join(", ", Enum.GetNames<DiaSemana>())}");

            var existente = await _repository.ObterPorBarbeiroEDiaAsync(barbeiroId, dia);
            if (existente is not null)
                throw new Exception($"Já existe disponibilidade cadastrada para {diaSemana}.");

            if (!TimeSpan.TryParse(horarioInicio, out var inicio))
                throw new Exception("Horário de início inválido. Use o formato HH:mm.");

            if (!TimeSpan.TryParse(horarioFim, out var fim))
                throw new Exception("Horário de fim inválido. Use o formato HH:mm.");

            if (inicio >= fim)
                throw new Exception("Horário de início deve ser menor que o horário de fim.");

            var disponibilidade = new DisponibilidadeBarbeiro(barbeiroId, dia, inicio, fim);
            await _repository.AdicionarAsync(disponibilidade);
            return disponibilidade;

        }
        public async Task Atualizar(Guid id, string horarioInicio, string horarioFim, bool ativo)
        {
            var disponibilidade = await _repository.ObterPorBarbeiroEDiaAsync(id, default)
            ?? throw new Exception("Disponibilidade Não encontrada.");

            if (!TimeSpan.TryParse(horarioInicio, out var inicio))
                throw new Exception("Horário início inválido.");

            if (!TimeSpan.TryParse(horarioFim, out var fim))
                throw new Exception("Horário fim inválido.");

            disponibilidade.Atualizar(inicio, fim, ativo);
            await _repository.AtualizarAsync(disponibilidade);
        }

        public async Task<DisponibilidadeBarbeiro?> ObterPorDiaAsync(Guid barbeiroId, DiaSemana dia) =>
        await _repository.ObterPorBarbeiroEDiaAsync(barbeiroId, dia);

    }
}