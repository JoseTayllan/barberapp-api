namespace BarberApp.Application.DTOs;

public sealed record IniciarConexaoMercadoPagoResponse(string UrlAutorizacao, DateTimeOffset ExpiraEm);
