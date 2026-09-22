using BarberApp.Domain.Entities;
using BarberApp.Domain.Interfaces;

namespace BarberApp.Application.Services;

public class ExcecaoAgendaService
{
    private readonly IExcecaoAgendaRepository _repository;
    private readonly IBarbeiroRepository _barbeiroRepository;

    public ExcecaoAgendaService(
        IExcecaoAgendaRepository repository,
        IBarbeiroRepository barbeiroRepository)
    {
        _repository = repository;
        _barbeiroRepository = barbeiroRepository;
    }

    public async Task<IEnumerable<ExcecaoAgenda>> ListarPorBarbeiroAsync(Guid barbeiroId) =>
        await _repository.ObterPorBarbeiroAsync(barbeiroId);

    public async Task<ExcecaoAgenda> CriarAsync(Guid barbeiroId, string dataStr,
        bool naoAtende, string? horarioInicio, string? horarioFim, string? motivo)
    {
        var barbeiro = await _barbeiroRepository.ObterPorIdAsync(barbeiroId)
            ?? throw new Exception("Barbeiro não encontrado.");

        var formatos = new[] { "dd/MM/yyyy", "yyyy-MM-dd" };
        if (!DateTime.TryParseExact(dataStr, formatos,
            System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.None, out var data))
            throw new Exception("Formato de data inválido. Use dd/MM/yyyy.");

        var dataUtc = DateTime.SpecifyKind(data.Date, DateTimeKind.Utc);

        var existente = await _repository.ObterPorBarbeiroEDataAsync(barbeiroId, dataUtc);
        if (existente is not null)
            throw new Exception("Já existe uma exceção para esta data. Use o endpoint de atualização.");

        TimeSpan? inicio = null;
        TimeSpan? fim = null;

        if (!naoAtende)
        {
            if (string.IsNullOrEmpty(horarioInicio) || string.IsNullOrEmpty(horarioFim))
                throw new Exception("Horário de início e fim são obrigatórios quando o barbeiro atende.");

            if (!TimeSpan.TryParse(horarioInicio, out var i))
                throw new Exception("Horário de início inválido.");

            if (!TimeSpan.TryParse(horarioFim, out var f))
                throw new Exception("Horário de fim inválido.");

            if (i >= f)
                throw new Exception("Horário de início deve ser menor que o horário de fim.");

            inicio = i;
            fim = f;
        }

        var excecao = new ExcecaoAgenda(barbeiroId, dataUtc, naoAtende, inicio, fim, motivo);
        await _repository.AdicionarAsync(excecao);
        return excecao;
    }

    public async Task<ExcecaoAgenda> AtualizarAsync(Guid barbeiroId, string dataStr,
        bool naoAtende, string? horarioInicio, string? horarioFim, string? motivo)
    {
        var formatos = new[] { "dd/MM/yyyy", "yyyy-MM-dd" };
        if (!DateTime.TryParseExact(dataStr, formatos,
            System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.None, out var data))
            throw new Exception("Formato de data inválido.");

        var dataUtc = DateTime.SpecifyKind(data.Date, DateTimeKind.Utc);

        var excecao = await _repository.ObterPorBarbeiroEDataAsync(barbeiroId, dataUtc)
            ?? throw new Exception("Exceção não encontrada para esta data.");

        TimeSpan? inicio = null;
        TimeSpan? fim = null;

        if (!naoAtende)
        {
            if (!TimeSpan.TryParse(horarioInicio, out var i))
                throw new Exception("Horário de início inválido.");
            if (!TimeSpan.TryParse(horarioFim, out var f))
                throw new Exception("Horário de fim inválido.");
            inicio = i;
            fim = f;
        }

        excecao.Atualizar(naoAtende, inicio, fim, motivo);
        await _repository.AtualizarAsync(excecao);
        return excecao;
    }

    public async Task RemoverAsync(Guid barbeiroId, string dataStr)
    {
        var formatos = new[] { "dd/MM/yyyy", "yyyy-MM-dd" };
        if (!DateTime.TryParseExact(dataStr, formatos,
            System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.None, out var data))
            throw new Exception("Formato de data inválido.");

        var dataUtc = DateTime.SpecifyKind(data.Date, DateTimeKind.Utc);

        var excecao = await _repository.ObterPorBarbeiroEDataAsync(barbeiroId, dataUtc)
            ?? throw new Exception("Exceção não encontrada para esta data.");

        await _repository.RemoverAsync(excecao);
    }

    public async Task<ExcecaoAgenda?> ObterPorDataAsync(Guid barbeiroId, DateTime data) =>
        await _repository.ObterPorBarbeiroEDataAsync(barbeiroId, data);
}