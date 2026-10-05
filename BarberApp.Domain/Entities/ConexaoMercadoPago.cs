namespace BarberApp.Domain.Entities;

public class ConexaoMercadoPago
{
    public int Id { get; private set; } = 1;
    public long ContaId { get; private set; }
    public string AdministradorId { get; private set; } = null!;
    public string TokensProtegidos { get; private set; } = null!;
    public DateTimeOffset ExpiraEm { get; private set; }
    public bool Sandbox { get; private set; }

    protected ConexaoMercadoPago() { }

    public ConexaoMercadoPago(long contaId, string administradorId, string tokensProtegidos,
        DateTimeOffset expiraEm, bool sandbox)
    {
        if (contaId <= 0) throw new ArgumentOutOfRangeException(nameof(contaId));
        ArgumentException.ThrowIfNullOrWhiteSpace(administradorId);
        ArgumentException.ThrowIfNullOrWhiteSpace(tokensProtegidos);
        ContaId = contaId;
        AdministradorId = administradorId;
        TokensProtegidos = tokensProtegidos;
        ExpiraEm = expiraEm;
        Sandbox = sandbox;
    }

    public void Atualizar(ConexaoMercadoPago novaConexao)
    {
        ArgumentNullException.ThrowIfNull(novaConexao);
        ContaId = novaConexao.ContaId;
        AdministradorId = novaConexao.AdministradorId;
        TokensProtegidos = novaConexao.TokensProtegidos;
        ExpiraEm = novaConexao.ExpiraEm;
        Sandbox = novaConexao.Sandbox;
    }
}
