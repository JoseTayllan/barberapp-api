using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace BarberApp.TestSupport.DataProtectionProbe;

internal static class WindowsProfileScenario
{
    private const string Confirmacao = "--confirmar-leitura-do-perfil-windows";
    private const string Proposito = "BarberApp.MercadoPago.Tokens.v1";
    private const string TextoFicticio = "ONLY-FOR-TESTS-NOT-A-MERCADO-PAGO-TOKEN";

    public static int Executar(string[] args)
    {
        var recusa = Validar(args);
        if (recusa is not null) return Recusar(recusa);
        if (args[0] != "perfil-inspecionar" && !OperatingSystem.IsWindows())
            return Recusar("PlataformaNaoSuportada");

        // Uma única política para inspeção/proteção/recuperação, sem gerar novas chaves.
        using var factory = new ApiProbeFactory(args[3], keyring: null, somenteLeitura: true);
        var services = factory.Services;
        var chaves = services.GetRequiredService<IOptions<KeyManagementOptions>>().Value;
        var identidade = services.GetRequiredService<IOptions<DataProtectionOptions>>().Value.ApplicationDiscriminator;
        if (chaves.AutoGenerateKeys || chaves.XmlRepository is not null || chaves.XmlEncryptor is not null
            || string.IsNullOrWhiteSpace(identidade))
            throw new InvalidOperationException("Política do diagnóstico incompatível com leitura do perfil.");

        var identidadeHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(identidade)));
        if (args[0] == "perfil-inspecionar")
        {
            // Somente opções: não chama Protect/Unprotect nem enumera arquivos/chaves.
            Console.WriteLine(JsonSerializer.Serialize(new
            {
                origem = "ApiServices",
                status = "Inspecionado",
                geracaoAutomatica = chaves.AutoGenerateKeys,
                repositorioExplicito = chaves.XmlRepository is not null,
                criptografiaExplicita = chaves.XmlEncryptor is not null,
                identidadeHash
            }));
            return 0;
        }

        var protector = services.GetRequiredService<IDataProtectionProvider>().CreateProtector(Proposito);
        if (args[0] == "perfil-proteger")
        {
            var payload = protector.Protect(TextoFicticio);
            // CreateNew também recusa criação concorrente após a checagem inicial.
            using (var arquivo = new FileStream(args[2], FileMode.CreateNew, FileAccess.Write, FileShare.None))
            using (var writer = new StreamWriter(arquivo))
                writer.Write(payload);
            Informar("Protegido", identidadeHash);
            return 0;
        }

        string recuperado;
        var ciphertext = File.ReadAllText(args[2]);
        try
        {
            recuperado = protector.Unprotect(ciphertext);
        }
        catch (CryptographicException)
        {
            Informar("Rejeitado", identidadeHash);
            return 3;
        }

        if (!string.Equals(TextoFicticio, recuperado, StringComparison.Ordinal)) return 4;
        Informar("Recuperado", identidadeHash);
        return 0;
    }

    private static string? Validar(string[] args)
    {
        if (!string.Equals(args[1], Confirmacao, StringComparison.Ordinal)) return "ConfirmacaoNecessaria";
        if (!string.Equals(args[4], Proposito, StringComparison.Ordinal)) return "PropositoInvalido";
        if (!Path.IsPathFullyQualified(args[3]) || !Directory.Exists(args[3]) || !SemLinks(args[3]))
            return "ContextoInvalido";
        if (!DestinoPermitido(args[2])) return "DestinoInvalido";

        var arquivo = new FileInfo(args[2]);
        if (arquivo.LinkTarget is not null || Directory.Exists(args[2])) return "DestinoInvalido";
        if (args[0] == "perfil-proteger" && arquivo.Exists) return "DestinoJaExiste";
        if (args[0] == "perfil-recuperar" && !arquivo.Exists) return "PayloadAusente";
        if (args[0] == "perfil-recuperar" && arquivo.Length > 16 * 1024) return "PayloadInvalido";
        return null;
    }

    private static bool DestinoPermitido(string caminho)
    {
        if (!Path.IsPathFullyQualified(caminho)) return false;
        var arquivo = new FileInfo(Path.GetFullPath(caminho));
        var pasta = arquivo.Directory;
        var comparacao = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        return arquivo.Name == "payload-protegido.txt" && pasta is { Exists: true, Parent: not null }
            && pasta.Name.StartsWith("barberapp-profile-probe-", StringComparison.Ordinal)
            && string.Equals(Path.TrimEndingDirectorySeparator(pasta.Parent.FullName),
                Path.TrimEndingDirectorySeparator(Path.GetFullPath(Path.GetTempPath())), comparacao)
            && SemLinks(pasta.FullName);
    }

    private static bool SemLinks(string caminho)
    {
        for (DirectoryInfo? pasta = new(Path.GetFullPath(caminho)); pasta is not null; pasta = pasta.Parent)
        {
            if (!pasta.Exists || (pasta.Attributes & FileAttributes.ReparsePoint) != 0) return false;
        }
        return true;
    }

    private static int Recusar(string status)
    {
        Console.WriteLine(JsonSerializer.Serialize(new { status }));
        return 6;
    }

    private static void Informar(string status, string identidadeHash) =>
        Console.WriteLine(JsonSerializer.Serialize(new
        {
            origem = "ApiServices",
            status,
            processoId = Environment.ProcessId,
            identidadeHash
        }));
}
