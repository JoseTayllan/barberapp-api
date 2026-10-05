namespace BarberApp.API.Middleware;

public static class MercadoPagoCallbackEndpoint
{
    public const string Path = "/api/integracoes/mercado-pago/callback";

    public static bool IsCallback(string? path, string? method) =>
        HttpMethods.IsGet(method ?? string.Empty)
        && string.Equals(path, Path, StringComparison.OrdinalIgnoreCase);
}
