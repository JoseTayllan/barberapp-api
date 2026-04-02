using System.Text.Json.Serialization;

namespace BarberApp.Application.DTOs;

public record RegistroRequest(
    string NomeCompleto,
    string Email,
    string Telefone,
    string Password
);

public record LoginRequest(
    string Email,
    string Password
);

public record AuthResponse(
    string Token,
    string Nome,
    string Email,
    IList<string> Roles,
    DateTime ExpiraEm
);