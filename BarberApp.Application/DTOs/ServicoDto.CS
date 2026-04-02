namespace BarberApp.Application.DTOs;

public record ServicoResponse(
    Guid Id,
    string Nome,
    string? Descricao,
    decimal Preco,
    int DuracaoMinutos
);

public record CriarServicoRequest(
    string Nome,
    decimal Preco,
    int DuracaoMinutos,
    string? Descricao
);