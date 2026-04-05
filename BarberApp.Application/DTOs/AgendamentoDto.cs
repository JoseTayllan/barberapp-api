namespace BarberApp.Application.DTOs;

public record AgendamentoResponse(
    Guid Id,
    string NomeCliente,
    string NomeBarbeiro,
    string NomeServico,
    decimal PrecoServico,
    DateTime DataHora,
    string Status
);

public record CriarAgendamentoRequest(
    Guid BarbeiroId,
    Guid ServicoId,
    DateTime DataHora,
    string? Observacao
);