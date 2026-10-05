namespace BarberApp.Infrastructure.Payment;

// Classe, não record: o ToString padrão não revela o verifier.
internal sealed class TentativaMercadoPago(
    string administradorId, string state, string verifier, DateTimeOffset expiraEm)
{
    public string AdministradorId { get; } = administradorId;
    public string State { get; } = state;
    public string Verifier { get; } = verifier;
    public DateTimeOffset ExpiraEm { get; } = expiraEm;
}
