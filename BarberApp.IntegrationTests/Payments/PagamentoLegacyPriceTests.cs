using BarberApp.Application.Services;
using BarberApp.Domain.Entities;
using BarberApp.Domain.Enums;
using BarberApp.Infrastructure.Data;
using BarberApp.Infrastructure.Payment;
using BarberApp.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BarberApp.IntegrationTests.Payments;

public sealed class PagamentoLegacyPriceTests
{
    [Fact]
    public async Task MockLegado_DevePreservarCalculoAnterior_SemInventarPrecoHistorico()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"PagamentoLegacyPrice-{Guid.NewGuid()}").Options;
        await using var context = new AppDbContext(options);
        var cliente = new Cliente("ONLY-FOR-TESTS-CLIENT", "cliente@example.test", "11999990000");
        var barbeiro = new Barbeiro("ONLY-FOR-TESTS-BARBER", "11999990001", null);
        var servico = new Servico("ONLY-FOR-TESTS-SERVICE", 40m, 30);
        var legado = new Agendamento(cliente.Id, barbeiro.Id, servico.Id,
            new DateTime(2030, 1, 7, 13, 0, 0, DateTimeKind.Utc));
        context.AddRange(cliente, barbeiro, servico, legado);
        await context.SaveChangesAsync();
        servico.Atualizar(servico.Nome, 50m, servico.DuracaoMinuto, servico.Descricao);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var service = new PagamentoService(new PagamentoRepository(context),
            new AgendamentoRepository(context), new MockPaymentService());
        var result = await service.ProcessarAsync(legado.Id, cliente.Email);
        Assert.Equal(50m, result.Valor);
        Assert.Equal("Mock", result.Gateway);
        Assert.Equal(StatusPagemento.Aprovado, result.Status);
        Assert.StartsWith("MOCK-", result.GatewayTransacaoId);
        var row = await context.Agendamentos.SingleAsync();
        Assert.Null(context.Entry(row).Property<decimal?>("PrecoAgendado").CurrentValue);
    }
}
