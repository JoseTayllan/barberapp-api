namespace BarberApp.Application.Services;

public sealed class MercadoPagoCallbackInvalidoException : Exception
{
    public MercadoPagoCallbackInvalidoException() : base("MercadoPagoCallbackInvalido") { }
}
