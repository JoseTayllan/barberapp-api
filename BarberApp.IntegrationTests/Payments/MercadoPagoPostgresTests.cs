using BarberApp.Domain.Entities;
using BarberApp.Infrastructure.Data;
using BarberApp.Infrastructure.Repositories;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Xunit;

namespace BarberApp.IntegrationTests.Payments;

public sealed class MercadoPagoPostgresTests
{
    [PostgresFact]
    public async Task Migration_DevePersistirCiphertextEImpedirSegundaInstalacao_NoPostgres()
    {
        var connection = new NpgsqlConnectionStringBuilder(Environment.GetEnvironmentVariable("BARBERAPP_TEST_POSTGRES"));
        // Este teste nunca deve migrar o banco real da aplicação.
        Assert.StartsWith("barberapp_test_", connection.Database);
        Assert.Equal("/var/run/postgresql", connection.Host);
        var options = new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(connection.ConnectionString).Options;
        await using var context = new AppDbContext(options);
        Assert.Contains(context.Database.GetMigrations(), name => name.EndsWith("_AddConexaoMercadoPago", StringComparison.Ordinal));
        await context.Database.MigrateAsync();

        await using var transaction = await context.Database.BeginTransactionAsync();
        var protector = new EphemeralDataProtectionProvider().CreateProtector("BarberApp.MercadoPago.Tokens.v1");
        const string fakePayload = "ONLY-FOR-TESTS-POSTGRES-TOKEN";
        var encrypted = protector.Protect(fakePayload);
        var conexao = new ConexaoMercadoPago(123, "postgres-test-admin", encrypted,
            new DateTimeOffset(2030, 1, 1, 0, 0, 0, TimeSpan.Zero), true);
        var repository = new ConexaoMercadoPagoRepository(context);
        await repository.SalvarAsync(conexao, CancellationToken.None);
        context.ChangeTracker.Clear();
        var row = await context.ConexoesMercadoPago.AsNoTracking().SingleAsync();
        Assert.DoesNotContain(fakePayload, row.TokensProtegidos);
        Assert.Equal(fakePayload, protector.Unprotect(row.TokensProtegidos));

        var exception = await Assert.ThrowsAsync<PostgresException>(() => context.Database.ExecuteSqlRawAsync("""
            INSERT INTO "ConexoesMercadoPago" ("Id", "ContaId", "AdministradorId", "TokensProtegidos", "ExpiraEm", "Sandbox")
            VALUES (2, 456, 'postgres-test-admin', 'ONLY-FOR-TESTS-CIPHERTEXT', '2030-01-01T00:00:00Z', true)
            """));
        Assert.Equal(PostgresErrorCodes.CheckViolation, exception.SqlState);
        Assert.Equal("CK_ConexoesMercadoPago_UnicaInstalacao", exception.ConstraintName);
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
