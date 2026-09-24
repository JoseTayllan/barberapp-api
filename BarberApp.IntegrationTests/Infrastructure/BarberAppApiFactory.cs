using BarberApp.API.Controllers;
using BarberApp.Infrastructure.Data;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace BarberApp.IntegrationTests.Infrastructure;

public sealed class BarberAppApiFactory : WebApplicationFactory<ServicosController>
{
    public const string ApiKey = "integration-tests-api-key";
    private readonly string _environmentName;

    public BarberAppApiFactory()
        : this("Testing")
    {
    }

    internal BarberAppApiFactory(string environmentName)
    {
        _environmentName = environmentName;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(_environmentName);
        builder.UseSetting("ApiKey:Value", ApiKey);
        builder.UseSetting(
            "JwtSettings:SecretKey",
            "integration-tests-secret-key-with-at-least-32-characters");
        builder.UseSetting("JwtSettings:Issuer", "BarberApp.IntegrationTests");
        builder.UseSetting("JwtSettings:Audience", "BarberApp.IntegrationTests");
        builder.UseSetting("JwtSettings:ExpiracaoHoras", "1");
        builder.ConfigureLogging(logging => logging.ClearProviders());

        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ApiKey:Value"] = ApiKey,
                ["JwtSettings:SecretKey"] = "integration-tests-secret-key-with-at-least-32-characters",
                ["JwtSettings:Issuer"] = "BarberApp.IntegrationTests",
                ["JwtSettings:Audience"] = "BarberApp.IntegrationTests",
                ["JwtSettings:ExpiracaoHoras"] = "1"
            });
        });

        builder.ConfigureServices(services =>
        {
            services.AddDataProtection().UseEphemeralDataProtectionProvider();

            services.RemoveAll<AppDbContext>();
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<AppDbContext>>();

            services.AddDbContext<AppDbContext>(options =>
                options.UseInMemoryDatabase($"BarberAppTests-{Guid.NewGuid()}"));
        });
    }
}
