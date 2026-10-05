using BarberApp.Domain.Entities;
using Xunit;

namespace BarberApp.UnitTests.Domain;

public sealed class ConexaoMercadoPagoTests
{
    private static readonly DateTimeOffset ExpiraEm = new(2030, 1, 1, 0, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData(0, "admin", "fake-protected-payload")]
    [InlineData(-1, "admin", "fake-protected-payload")]
    [InlineData(123, null, "fake-protected-payload")]
    [InlineData(123, "  ", "fake-protected-payload")]
    [InlineData(123, "admin", null)]
    [InlineData(123, "admin", "  ")]
    public void Construtor_DeveRejeitarDadosInvalidos(long contaId, string? admin, string? protegido)
    {
        Assert.ThrowsAny<ArgumentException>(() => new ConexaoMercadoPago(contaId, admin!, protegido!, ExpiraEm, true));
    }

    [Fact]
    public void Atualizar_DevePreservarEstado_QuandoNovaConexaoForNula()
    {
        var conexao = new ConexaoMercadoPago(123, "admin", "fake-protected-payload", ExpiraEm, true);
        Assert.Throws<ArgumentNullException>(() => conexao.Atualizar(null!));
        Assert.Equal(123, conexao.ContaId);
        Assert.Equal("admin", conexao.AdministradorId);
        Assert.Equal("fake-protected-payload", conexao.TokensProtegidos);
    }

    [Fact]
    public void Atualizar_DeveSubstituirDados_SemMudarIdentificadorDaInstalacao()
    {
        var conexao = new ConexaoMercadoPago(123, "admin", "fake-protected-payload", ExpiraEm, true);
        var nova = new ConexaoMercadoPago(456, "other-admin", "other-protected-payload", ExpiraEm.AddDays(1), false);
        conexao.Atualizar(nova);
        Assert.Equal(1, conexao.Id);
        Assert.Equal(456, conexao.ContaId);
        Assert.Equal("other-admin", conexao.AdministradorId);
        Assert.Equal("other-protected-payload", conexao.TokensProtegidos);
        Assert.Equal(ExpiraEm.AddDays(1), conexao.ExpiraEm);
        Assert.False(conexao.Sandbox);
    }
}
