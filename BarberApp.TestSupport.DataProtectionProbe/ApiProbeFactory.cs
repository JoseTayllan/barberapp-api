using BarberApp.API.Controllers;
using BarberApp.Infrastructure.Data;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace BarberApp.TestSupport.DataProtectionProbe;

// Inicializa o entry point real, mas não lê configuração privada nem conecta ao PostgreSQL.
internal sealed class ApiProbeFactory(string contentRoot, string? keyring, bool somenteLeitura)
    : WebApplicationFactory<ServicosController>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        var valores = new Dictionary<string, string?>
        {
            ["ApiKey:Value"] = "ONLY-FOR-TESTS-PROBE-API-KEY",
            ["JwtSettings:SecretKey"] = "ONLY-FOR-TESTS-PROBE-JWT-SECRET-AT-LEAST-32-CHARACTERS",
            ["JwtSettings:Issuer"] = "BarberApp.ProtectionProbe",
            ["JwtSettings:Audience"] = "BarberApp.ProtectionProbe",
            ["JwtSettings:ExpiracaoHoras"] = "1",
            ["BootstrapAdmin:Enabled"] = "false",
            ["MercadoPago:ConexaoEnabled"] = "false"
        };
        builder.UseEnvironment("ProtectionProbe");
        builder.UseContentRoot(contentRoot);
        foreach (var valor in valores) builder.UseSetting(valor.Key, valor.Value);
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.Sources.Clear();
            configuration.AddInMemoryCollection(valores);
        });
        builder.ConfigureLogging(logging => logging.ClearProviders());
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<AppDbContext>();
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<AppDbContext>>();
            services.AddDbContext<AppDbContext>(options =>
                options.UseInMemoryDatabase($"ProtectionProbe-{Guid.NewGuid()}"));

            // Null preserva a seleção padrão do perfil; chave fictícia usa diretório explícito.
            var protection = services.AddDataProtection();
            if (keyring is not null)
            {
                protection.PersistKeysToFileSystem(new DirectoryInfo(keyring));
                if (OperatingSystem.IsWindows()) protection.ProtectKeysWithDpapi();
            }
            if (somenteLeitura) protection.DisableAutomaticKeyGeneration();
        });
    }
}
