namespace BarberApp.Domain.Entities;

public sealed record MetadadosConexaoMercadoPago(
    long ContaId, DateTimeOffset ExpiraEm, bool Sandbox, bool RequerReautorizacao);
