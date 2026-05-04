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
    public record DisponibildadeResponse(
        Guid Id,
        string DiaSemana,
        string HorarioInicio,
        string HorarioFim,
        bool Ativo
    );
    public record CriarDisponibilidadeRequest(
        string DiaSemana,
        string HorarioInicio,
        string HorarioFim
    );
    public record AtuaizarDisponibilidadeRequest(
        string HorarioInicio,
        string HorarioFim,
        bool Ativo);
    public record CriarBarbeiroComLoginRequest(
        string NomeCompleto,
        string Email,
        string Telefone,
        string Senha,
        string? Foto
    );
    public record BarberiroComLoginRespnse(
        Guid BarbeiroId,
        string UserId,
        string Nome,
        string Email,
        string Telefone
    );
}