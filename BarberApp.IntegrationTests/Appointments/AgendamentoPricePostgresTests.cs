using BarberApp.Domain.Entities;
using BarberApp.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;
using Xunit;

namespace BarberApp.IntegrationTests.Appointments;

public sealed class AgendamentoPricePostgresTests
{
    [PostgresFact]
    public async Task PrecoAgendado_DevePersistirCentavosENuloLegadoSemRecalcular_NoPostgres()
    {
        var connection = new NpgsqlConnectionStringBuilder(Environment.GetEnvironmentVariable("BARBERAPP_TEST_POSTGRES"));
        Assert.StartsWith("barberapp_test_", connection.Database);
        Assert.Equal("/var/run/postgresql", connection.Host);
        var options = new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(connection.ConnectionString).Options;
        await using var context = new AppDbContext(options);
        await context.Database.MigrateAsync();
        await context.Database.OpenConnectionAsync();

        await using (var command = context.Database.GetDbConnection().CreateCommand())
        {
            command.CommandText = """
                SELECT data_type, numeric_precision, numeric_scale, is_nullable, column_default
                FROM information_schema.columns
                WHERE table_schema = 'public' AND table_name = 'Agendamentos' AND column_name = 'PrecoAgendado'
                """;
            await using var reader = await command.ExecuteReaderAsync();
            Assert.True(await reader.ReadAsync(), "Migration deve criar a coluna de preço combinado.");
            Assert.Equal("numeric", reader.GetString(0));
            Assert.Equal(10, reader.GetInt32(1));
            Assert.Equal(2, reader.GetInt32(2));
            Assert.Equal("YES", reader.GetString(3));
            Assert.True(reader.IsDBNull(4), "Não inventar preço histórico com default/backfill.");
            Assert.False(await reader.ReadAsync());
        }

        await using var transaction = await context.Database.BeginTransactionAsync();
        var cliente = new Cliente("ONLY-FOR-TESTS-CLIENT", $"{Guid.NewGuid():N}@example.test", "11999990000");
        var barbeiro = new Barbeiro("ONLY-FOR-TESTS-BARBER", "11999990001", null);
        var servico = new Servico("ONLY-FOR-TESTS-SERVICE", 40.99m, 30);
        context.AddRange(cliente, barbeiro, servico);
        await context.SaveChangesAsync();

        // INSERT legado omite a nova coluna: precisa continuar válido e resultar em null.
        var legacyId = Guid.NewGuid();
        await context.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO "Agendamentos"
                ("Id", "ClienteId", "BarbeiroId", "ServicoId", "DataHora", "Status", "CridoEm")
            VALUES ({legacyId}, {cliente.Id}, {barbeiro.Id}, {servico.Id},
                {new DateTime(2030, 1, 7, 13, 0, 0, DateTimeKind.Utc)}, 'Pendente', {DateTime.UtcNow})
            """);
        var current = new Agendamento(cliente.Id, barbeiro.Id, servico.Id,
            new DateTime(2030, 1, 7, 14, 0, 0, DateTimeKind.Utc));
        context.Agendamentos.Add(current);
        context.Entry(current).Property<decimal?>("PrecoAgendado").CurrentValue = 40.99m;
        await context.SaveChangesAsync();
        servico.Atualizar(servico.Nome, 50m, servico.DuracaoMinuto, servico.Descricao);
        await context.SaveChangesAsync();

        var freshOptions = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(context.Database.GetDbConnection()).Options;
        await using (var fresh = new AppDbContext(freshOptions))
        {
            await fresh.Database.UseTransactionAsync(transaction.GetDbTransaction());
            var legacyPrice = await fresh.Agendamentos.Where(row => row.Id == legacyId)
                .Select(row => EF.Property<decimal?>(row, "PrecoAgendado")).SingleAsync();
            var frozenPrice = await fresh.Agendamentos.Where(row => row.Id == current.Id)
                .Select(row => EF.Property<decimal?>(row, "PrecoAgendado")).SingleAsync();
            Assert.Null(legacyPrice);
            Assert.Equal(40.99m, frozenPrice);
            Assert.Equal(50m, await fresh.Servicos.Where(row => row.Id == servico.Id).Select(row => row.Preco).SingleAsync());
        }

        foreach (var invalidPrice in new[] { 0m, -1m })
        {
            await transaction.CreateSavepointAsync("invalid_price");
            var exception = await Assert.ThrowsAsync<PostgresException>(() => context.Database.ExecuteSqlInterpolatedAsync(
                $"UPDATE \"Agendamentos\" SET \"PrecoAgendado\" = {invalidPrice} WHERE \"Id\" = {current.Id}"));
            Assert.Equal(PostgresErrorCodes.CheckViolation, exception.SqlState);
            Assert.Equal("CK_Agendamentos_PrecoAgendadoPositivo", exception.ConstraintName);
            await transaction.RollbackToSavepointAsync("invalid_price");
        }
        await transaction.RollbackAsync();
    }

    private sealed class PostgresFactAttribute : FactAttribute
    {
        public PostgresFactAttribute()
        {
            if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("BARBERAPP_TEST_POSTGRES")))
                Skip = "Defina BARBERAPP_TEST_POSTGRES para o banco isolado WSL; nunca use o banco real.";
        }
    }
}
