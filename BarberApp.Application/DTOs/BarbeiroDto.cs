namespace BarberApp.Application.DTOs
{
    public record BarbeiroResponse(
        Guid Id,
        string Nome,
        string Telefone,
        string? Foto
    );

    public record BarbeiroCreateRequest(
        string Nome,
        string Telefone,
        string? Foto
    );
}