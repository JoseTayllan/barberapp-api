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

public record PerfilResponse(
    string Id,
    string NomeCompleto,
    string Email,
    string Telefone,
    IList<string> Roles
);

public record AtualizarPerfilRequest(
    string NomeCompleto,
    string Telefone
);

public record AlterarSenhaRequest(
    string SenhaAtual,
    string NovaSenha
);