namespace BarberApp.Application.Services;

public sealed class MercadoPagoNaoConfiguradoException : Exception
{
    public MercadoPagoNaoConfiguradoException() : base("MercadoPagoNaoConfigurado") { }
}
