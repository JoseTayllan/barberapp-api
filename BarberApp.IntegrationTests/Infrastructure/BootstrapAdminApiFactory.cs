using BarberApp.API.Controllers;
using BarberApp.Infrastructure.Data;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace BarberApp.IntegrationTests.Infrastructure;

internal sealed class BootstrapAdminApiFactory : WebApplicationFactory<ServicosController>
{
    private readonly IReadOnlyDictionary<string, string?> _bootstrapAdminConfiguration;
    private readonly string _databaseName;
    private readonly InMemoryDatabaseRoot _databaseRoot;
    private readonly Action<IServiceCollection>? _configureServices;

    public BootstrapAdminApiFactory(
        IReadOnlyDictionary<string, string?>? bootstrapAdminConfiguration = null,
        Action<AppDbContext>? seedDatabase = null,
        string? databaseName = null,
        InMemoryDatabaseRoot? databaseRoot = null,
        Action<IServiceCollection>? configureServices = null)
    {
        _bootstrapAdminConfiguration = bootstrapAdminConfiguration
            ?? new Dictionary<string, string?>();
        _databaseName = databaseName ?? $"BootstrapAdminTests-{Guid.NewGuid()}";
        _databaseRoot = databaseRoot ?? new InMemoryDatabaseRoot();
        _configureServices = configureServices;

        if (seedDatabase is not null)
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(_databaseName, _databaseRoot)
                .Options;

            using var dbContext = new AppDbContext(options);
            seedDatabase(dbContext);
            dbContext.SaveChanges();
        }
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ApiKey:Value", BarberAppApiFactory.ApiKey);
        builder.UseSetting(
            "JwtSettings:SecretKey",
            "integration-tests-secret-key-with-at-least-32-characters");
        builder.UseSetting("JwtSettings:Issuer", "BarberApp.IntegrationTests");
        builder.UseSetting("JwtSettings:Audience", "BarberApp.IntegrationTests");
        builder.UseSetting("JwtSettings:ExpiracaoHoras", "1");

        foreach (var setting in _bootstrapAdminConfiguration)
        {
            if (setting.Value is not null)
            {
                builder.UseSetting(setting.Key, setting.Value);
            }
        }

        builder.ConfigureLogging(logging => logging.ClearProviders());

        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            var values = new Dictionary<string, string?>
            {
                ["ApiKey:Value"] = BarberAppApiFactory.ApiKey,
                ["JwtSettings:SecretKey"] = "integration-tests-secret-key-with-at-least-32-characters",
                ["JwtSettings:Issuer"] = "BarberApp.IntegrationTests",
                ["JwtSettings:Audience"] = "BarberApp.IntegrationTests",
                ["JwtSettings:ExpiracaoHoras"] = "1"
            };

            foreach (var setting in _bootstrapAdminConfiguration)
            {
                values[setting.Key] = setting.Value;
            }

            configuration.AddInMemoryCollection(values);
        });

        builder.ConfigureServices(services =>
        {
            services.AddDataProtection().UseEphemeralDataProtectionProvider();

            services.RemoveAll<AppDbContext>();
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<AppDbContext>>();

            services.AddDbContext<AppDbContext>(options =>
                options.UseInMemoryDatabase(_databaseName, _databaseRoot));

            _configureServices?.Invoke(services);
        });
    }

    public AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(_databaseName, _databaseRoot)
            .Options;

        return new AppDbContext(options);
    }
}
