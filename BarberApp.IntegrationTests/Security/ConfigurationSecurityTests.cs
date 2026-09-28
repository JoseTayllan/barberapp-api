using Xunit;

namespace BarberApp.IntegrationTests.Security;

public sealed class ConfigurationSecurityTests
{
    [Fact]
    public void AppSettingsVersionado_NaoDeveConterSenhaNaConnectionString()
    {
        var repositoryRoot = Path.GetFullPath(
            Path.Combine(
                AppContext.BaseDirectory,
                "..",
                "..",
                "..",
                ".."));

        var appSettingsPath = Path.Combine(
            repositoryRoot,
            "BarberApp.API",
            "appsettings.json");

        Assert.True(
            File.Exists(appSettingsPath),
            $"O arquivo appsettings.json não foi encontrado no caminho: {appSettingsPath}");

        var appSettingsContent = File.ReadAllText(appSettingsPath);

        var contemSenha = appSettingsContent.Contains(
            "Password=",
            StringComparison.OrdinalIgnoreCase);

        Assert.False(
            contemSenha,
            "O arquivo appsettings.json contém uma senha na connection string. Isso não é seguro e deve ser evitado.");
    }
}
