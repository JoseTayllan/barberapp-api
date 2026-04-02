namespace BarberApp.Application.DTOs;

public record ClienteResponse(
    Guid Id,
    string Nome,
    string Email,
    string Telefone
);

public record CriarClienteRequest(
    string Nome,
    string Email,
    string Telefone
);