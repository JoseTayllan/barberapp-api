using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace BarberApp.TestSupport.DataProtectionProbe;

internal static class ApiProtectionScenario
{
    private const string TextoFicticio = "ONLY-FOR-TESTS-NOT-A-MERCADO-PAGO-TOKEN";

    public static int Executar(string[] args)
    {
        using var factory = new ApiProbeFactory(args[3], args[1], somenteLeitura: args[0] == "api-recuperar");
        var provider = factory.Services.GetRequiredService<IDataProtectionProvider>();
        var identidade = factory.Services.GetRequiredService<IOptions<DataProtectionOptions>>()
            .Value.ApplicationDiscriminator;
        if (string.IsNullOrWhiteSpace(identidade))
            throw new InvalidOperationException("Discriminador da API ausente na prova.");

        var identidadeHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(identidade)));
        var protector = provider.CreateProtector(args[4]);
        if (args[0] == "api-proteger")
        {
            File.WriteAllText(args[2], protector.Protect(TextoFicticio));
            Informar("Protegido", identidadeHash);
            return 0;
        }

        // Só a operação criptográfica pode produzir Rejeitado; falhas do host são infraestrutura.
        var payload = File.ReadAllText(args[2]);
        string recuperado;
        try
        {
            recuperado = protector.Unprotect(payload);
        }
        catch (CryptographicException)
        {
            Informar("Rejeitado", identidadeHash);
            return 3;
        }

        if (!string.Equals(TextoFicticio, recuperado, StringComparison.Ordinal))
            return 4;

        Informar("Recuperado", identidadeHash);
        return 0;
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
