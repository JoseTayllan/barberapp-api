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
}