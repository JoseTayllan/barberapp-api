using BarberApp.Domain.Entities;
using BarberApp.Infrastructure.Data;
using BarberApp.Infrastructure.Repositories;
using BarberApp.IntegrationTests.Infrastructure;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;
using Npgsql;
using Xunit;

namespace BarberApp.IntegrationTests.Payments;

public sealed class MercadoPagoPostgresTests
{
    [PostgresFact]
    public async Task Desconectar_DevePersistirExclusaoIdempotente_EntreEscoposNoPostgres()
    {
        var connection = new NpgsqlConnectionStringBuilder(Environment.GetEnvironmentVariable("BARBERAPP_TEST_POSTGRES"));
        Assert.StartsWith("barberapp_test_", connection.Database);
        Assert.Equal("/var/run/postgresql", connection.Host);
        var options = new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(connection.ConnectionString).Options;
        await using var context = new AppDbContext(options);
        await context.Database.MigrateAsync();
        await using var transaction = await context.Database.BeginTransactionAsync();
        context.ConexoesMercadoPago.Add(new ConexaoMercadoPago(123456, "ONLY-FOR-TESTS-ADMIN",
            "ONLY-FOR-TESTS-UNREADABLE-CIPHERTEXT", DateTimeOffset.UtcNow.AddHours(1), true));
        await context.SaveChangesAsync();

        using var provider = new ProvedorProibido();
        using var factory = new BarberAppApiFactory().WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(
                new Dictionary<string, string?> { ["MercadoPago:ConexaoEnabled"] = "false" }));
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<AppDbContext>();
                services.RemoveAll<DbContextOptions<AppDbContext>>();
                services.RemoveAll<IDbContextOptionsConfiguration<AppDbContext>>();
                services.AddDbContext<AppDbContext>(configuration => configuration.UseNpgsql(context.Database.GetDbConnection()));
                // Escopos distintos, mesma transação: desfaz todos os dados do teste ao final.
                services.AddScoped(serviceProvider =>
                {
                    var scoped = new AppDbContext(serviceProvider.GetRequiredService<DbContextOptions<AppDbContext>>());
                    scoped.Database.UseTransaction(transaction.GetDbTransaction());
                    return scoped;
                });
                services.RemoveAll<IHttpClientFactory>();
                services.AddSingleton<IHttpClientFactory>(provider);
            });
        });
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var jwt = new JwtSecurityToken(issuer: "BarberApp.IntegrationTests", audience: "BarberApp.IntegrationTests",
            claims: new[] { new Claim(ClaimTypes.Role, "Admin"), new Claim(ClaimTypes.NameIdentifier, "requesting-admin") },
            expires: DateTime.UtcNow.AddMinutes(5),
            signingCredentials: new SigningCredentials(new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes("integration-tests-secret-key-with-at-least-32-characters")), SecurityAlgorithms.HmacSha256));
        client.DefaultRequestHeaders.Add("X-API-Key", BarberAppApiFactory.ApiKey);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", new JwtSecurityTokenHandler().WriteToken(jwt));
        for (var attempt = 0; attempt < 2; attempt++)
        {
            using var response = await client.DeleteAsync("/api/integracoes/mercado-pago/conexao");
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.True(response.Headers.CacheControl?.NoStore);
            Assert.Empty(await response.Content.ReadAsStringAsync());
            using var scope = factory.Services.CreateScope();
            Assert.False(await scope.ServiceProvider.GetRequiredService<AppDbContext>().ConexoesMercadoPago.AnyAsync());
        }
        Assert.Equal(0, provider.Chamadas);
        await transaction.RollbackAsync();
    }

    [PostgresFact]
    public async Task Recuperacao_DevePersistirMarcadorNaoNuloComDefaultSeguro_EntreContextosPostgres()
    {
        var connection = new NpgsqlConnectionStringBuilder(Environment.GetEnvironmentVariable("BARBERAPP_TEST_POSTGRES"));
        Assert.StartsWith("barberapp_test_", connection.Database);
        Assert.Equal("/var/run/postgresql", connection.Host);
        var options = new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(connection.ConnectionString).Options;
        await using var context = new AppDbContext(options);
        var marker = context.Model.FindEntityType(typeof(ConexaoMercadoPago))!.FindProperty("RequerReautorizacao");
        Assert.NotNull(marker);
        Assert.Equal(typeof(bool), marker.ClrType);
        Assert.False(marker.IsNullable);
        await context.Database.MigrateAsync();
        await using var transaction = await context.Database.BeginTransactionAsync();

        // INSERT legado sem marcador deve continuar válido; migration precisa definir default false.
        await context.Database.ExecuteSqlRawAsync("""
            INSERT INTO "ConexoesMercadoPago" ("Id", "ContaId", "AdministradorId", "TokensProtegidos", "ExpiraEm", "Sandbox")
            VALUES (1, 123456, 'ONLY-FOR-TESTS-ADMIN', 'ONLY-FOR-TESTS-OPAQUE-CIPHERTEXT', '2030-01-01T00:00:00Z', true)
            """);
        var row = await context.ConexoesMercadoPago.SingleAsync();
        Assert.False(context.Entry(row).Property<bool>("RequerReautorizacao").CurrentValue);
        context.Entry(row).Property<bool>("RequerReautorizacao").CurrentValue = true;
        await context.SaveChangesAsync();

        var freshOptions = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(context.Database.GetDbConnection()).Options;
        await using var fresh = new AppDbContext(freshOptions);
        await fresh.Database.UseTransactionAsync(transaction.GetDbTransaction());
        var saved = await fresh.ConexoesMercadoPago.SingleAsync();
        Assert.True(fresh.Entry(saved).Property<bool>("RequerReautorizacao").CurrentValue);
        await transaction.RollbackAsync();
    }

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

    private sealed class ProvedorProibido : HttpMessageHandler, IHttpClientFactory
    {
        public int Chamadas { get; private set; }
        public HttpClient CreateClient(string name) => new(this, disposeHandler: false);
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Chamadas++;
            throw new InvalidOperationException("ONLY-FOR-TESTS-NO-REMOTE-CALL-ALLOWED");
        }
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
