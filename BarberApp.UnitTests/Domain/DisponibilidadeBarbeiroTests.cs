using BarberApp.Domain.Entities;
using BarberApp.Domain.Enums;
using Xunit;

namespace BarberApp.UnitTests.Domain;

public sealed class DisponibilidadeBarbeiroTests
{
    [Theory]
    [InlineData(-1, 600)]
    [InlineData(540, 540)]
    [InlineData(600, 540)]
    [InlineData(540, 1441)]
    public void Construtor_DeveRejeitarIntervalo_QuandoHorarioForInvalido(int inicio, int fim)
    {
        Assert.Throws<ArgumentException>(() => new DisponibilidadeBarbeiro(
            Guid.NewGuid(), DiaSemana.Segunda,
            TimeSpan.FromMinutes(inicio), TimeSpan.FromMinutes(fim)));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(7)]
    public void Construtor_DeveRejeitarDia_QuandoNaoExistirNoEnum(int dia)
    {
        Assert.Throws<ArgumentException>(() => new DisponibilidadeBarbeiro(
            Guid.NewGuid(), (DiaSemana)dia, TimeSpan.FromHours(9), TimeSpan.FromHours(18)));
    }

    [Theory]
    [InlineData(0, 60)]
    [InlineData(540, 1080)]
    [InlineData(1380, 1440)]
    public void Construtor_DeveAceitarIntervalo_QuandoEstiverDentroDoDia(int inicio, int fim)
    {
        var barbeiroId = Guid.NewGuid();
        var disponibilidade = new DisponibilidadeBarbeiro(
            barbeiroId, DiaSemana.Segunda,
            TimeSpan.FromMinutes(inicio), TimeSpan.FromMinutes(fim));

        Assert.Equal(barbeiroId, disponibilidade.BarbeiroId);
        Assert.Equal(TimeSpan.FromMinutes(inicio), disponibilidade.HoraInicio);
        Assert.Equal(TimeSpan.FromMinutes(fim), disponibilidade.HoraFim);
        Assert.True(disponibilidade.Ativo);
    }

    [Theory]
    [InlineData(-1, 600)]
    [InlineData(540, 540)]
    [InlineData(600, 540)]
    [InlineData(540, 1441)]
    public void Atualizar_DevePreservarEstado_QuandoIntervaloForInvalido(int inicio, int fim)
    {
        var disponibilidade = Criar();
        var atualizadoEm = disponibilidade.AtualizadoEm;

        Assert.Throws<ArgumentException>(() => disponibilidade.Atualizar(
            TimeSpan.FromMinutes(inicio), TimeSpan.FromMinutes(fim), false));

        Assert.Equal(TimeSpan.FromHours(9), disponibilidade.HoraInicio);
        Assert.Equal(TimeSpan.FromHours(18), disponibilidade.HoraFim);
        Assert.True(disponibilidade.Ativo);
        Assert.Equal(atualizadoEm, disponibilidade.AtualizadoEm);
    }

    [Fact]
    public void Atualizar_DeveAplicarValores_QuandoIntervaloForValido()
    {
        var disponibilidade = Criar();

        disponibilidade.Atualizar(TimeSpan.FromHours(10), TimeSpan.FromHours(17), false);

        Assert.Equal(TimeSpan.FromHours(10), disponibilidade.HoraInicio);
        Assert.Equal(TimeSpan.FromHours(17), disponibilidade.HoraFim);
        Assert.False(disponibilidade.Ativo);
        Assert.NotNull(disponibilidade.AtualizadoEm);
        Assert.Equal(DateTimeKind.Utc, disponibilidade.AtualizadoEm.Value.Kind);
    }

    private static DisponibilidadeBarbeiro Criar() => new(
        Guid.NewGuid(), DiaSemana.Segunda, TimeSpan.FromHours(9), TimeSpan.FromHours(18));
}
