using System.Security.Cryptography;
using Microsoft.AspNetCore.DataProtection;

namespace BarberApp.TestSupport.DataProtectionProbe;

// Executável exclusivo dos testes. O modo api-* usa a API com dependências isoladas.
internal static class Program
{
    private const string TextoFicticio = "ONLY-FOR-TESTS-NOT-A-MERCADO-PAGO-TOKEN";

    public static int Main(string[] args)
    {
        if (args.Length != 5 || args[0] is not ("proteger" or "recuperar" or "api-proteger" or "api-recuperar"))
            return 2;

        try
        {
            if (args[0] is "api-proteger" or "api-recuperar")
                return ApiProtectionScenario.Executar(args);

            var provider = DataProtectionProvider.Create(new DirectoryInfo(args[1]), builder =>
            {
                builder.SetApplicationName(args[3]);
                if (OperatingSystem.IsWindows())
                    builder.ProtectKeysWithDpapi();
                if (args[0] == "recuperar")
                    builder.DisableAutomaticKeyGeneration();
            });
            var protector = provider.CreateProtector(args[4]);

            if (args[0] == "proteger")
            {
                File.WriteAllText(args[2], protector.Protect(TextoFicticio));
                Console.WriteLine("PROTEGIDO");
                return 0;
            }

            var recuperado = protector.Unprotect(File.ReadAllText(args[2]));
            if (!string.Equals(TextoFicticio, recuperado, StringComparison.Ordinal))
                return 4;

            Console.WriteLine("RECUPERADO");
            return 0;
        }
        catch (CryptographicException) when (args[0] is not ("api-proteger" or "api-recuperar"))
        {
            Console.WriteLine("REJEITADO");
            return 3;
        }
        catch (Exception)
        {
            // Não imprimir payload, material de chave, caminhos ou detalhes internos.
            Console.Error.WriteLine("FALHA_INFRAESTRUTURA_PROBE");
            return 5;
        }
    }
}
