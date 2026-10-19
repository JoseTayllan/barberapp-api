namespace BarberApp.Application.DTOs;

public sealed record StatusConexaoMercadoPagoResponse(
    string Status, long? ContaId, DateTimeOffset? ExpiraEm, bool? Sandbox);
