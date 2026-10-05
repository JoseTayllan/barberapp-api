namespace BarberApp.Application.Services;

public sealed class MercadoPagoOperacaoException(string codigo) : Exception(codigo)
{
    public string Codigo { get; } = codigo;
}
