using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Xunit;

namespace BarberApp.IntegrationTests.Security;

// Contrato do modo de diagnóstico do auxiliar. Ainda não é execução com o perfil real.
public sealed class ApiDataProtectionProcessTests : IDisposable
{
    private const string Proposito = "BarberApp.MercadoPago.Tokens.v1";
    private readonly DirectoryInfo _temporario = Directory.CreateTempSubdirectory("barberapp-api-protection-tests-");

    [Fact]
    public async Task ProviderDaApi_DeveRecuperarTexto_EmOutroProcessoComMesmoContentRoot()
    {
        var primeiro = await ProtegerAsync();
        Assert.NotEmpty(Directory.GetFiles(PastaChaves, "key-*.xml"));
        var payload = await File.ReadAllTextAsync(ArquivoPayload);
        Assert.DoesNotContain("ONLY-FOR-TESTS-NOT-A-MERCADO-PAGO-TOKEN", payload);

        var segundo = await ExecutarAsync("api-recuperar", PastaChaves, ContentRoot, Proposito);
        var leitura = ValidarResultado(segundo, "Recuperado", codigo: 0);
        Assert.Equal(primeiro, leitura);
    }

    [Fact]
    public async Task ProviderDaApi_DeveRejeitarTexto_QuandoContentRootMudar()
    {
        var identidadeOriginal = await ProtegerAsync();
        var outroRoot = Directory.CreateDirectory(Path.Combine(_temporario.FullName, "outra-api")).FullName;

        var resultado = await ExecutarAsync("api-recuperar", PastaChaves, outroRoot, Proposito);
        var outraIdentidade = ValidarResultado(resultado, "Rejeitado", codigo: 3);
        Assert.NotEqual(identidadeOriginal, outraIdentidade);
    }

    [Fact]
    public async Task ProviderDaApi_DeveRejeitarTexto_QuandoPropositoMudar()
    {
        var identidade = await ProtegerAsync();
        var resultado = await ExecutarAsync("api-recuperar", PastaChaves, ContentRoot,
            "ONLY-FOR-TESTS-WRONG-PURPOSE");
        Assert.Equal(identidade, ValidarResultado(resultado, "Rejeitado", codigo: 3));
    }

    [Fact]
    public async Task ProviderDaApi_DeveRejeitarTextoSemGerarChaves_QuandoKeyringEstiverVazio()
    {
        var identidade = await ProtegerAsync();
        var pastaVazia = Directory.CreateDirectory(Path.Combine(_temporario.FullName, "keyring-vazio")).FullName;
        var resultado = await ExecutarAsync("api-recuperar", pastaVazia, ContentRoot, Proposito);

        Assert.Equal(identidade, ValidarResultado(resultado, "Rejeitado", codigo: 3));
        Assert.Empty(Directory.GetFiles(pastaVazia, "key-*.xml"));
    }

    private string PastaChaves => Path.Combine(_temporario.FullName, "chaves");
    private string ArquivoPayload => Path.Combine(_temporario.FullName, "payload-protegido.txt");
    private string ContentRoot => Path.Combine(_temporario.FullName, "api");

    private async Task<string> ProtegerAsync()
    {
        Directory.CreateDirectory(PastaChaves);
        Directory.CreateDirectory(ContentRoot);
        var resultado = await ExecutarAsync("api-proteger", PastaChaves, ContentRoot, Proposito);
        return ValidarResultado(resultado, "Protegido", codigo: 0);
    }

    private static string ValidarResultado(ResultadoProcesso resultado, string status, int codigo)
    {
        Assert.True(resultado.Codigo == codigo,
            $"Modo de diagnóstico da API: esperado código {codigo}, recebido {resultado.Codigo}. "
            + "Código 2 indica modo não suportado; código 5 indica falha de infraestrutura, não rejeição criptográfica.");
        Assert.Empty(resultado.Erro);
        using var documento = JsonDocument.Parse(resultado.Saida);
        var json = documento.RootElement;
        Assert.Equal(new[] { "identidadeHash", "origem", "processoId", "status" },
            json.EnumerateObject().Select(property => property.Name).OrderBy(name => name).ToArray());
        Assert.Equal("ApiServices", json.GetProperty("origem").GetString());
        Assert.Equal(status, json.GetProperty("status").GetString());
        Assert.Equal(resultado.ProcessoId, json.GetProperty("processoId").GetInt32());
        var identidade = Assert.IsType<string>(json.GetProperty("identidadeHash").GetString());
        Assert.Matches("^[A-F0-9]{64}$", identidade);
        Assert.DoesNotContain("ONLY-FOR-TESTS-NOT-A-MERCADO-PAGO-TOKEN", resultado.Saida);
        return identidade;
    }

    private async Task<ResultadoProcesso> ExecutarAsync(string modo, string pasta, string contentRoot, string proposito)
    {
        var probe = Path.Combine(AppContext.BaseDirectory, "ProtectionProbe",
            "BarberApp.TestSupport.DataProtectionProbe.dll");
        Assert.True(File.Exists(probe), "Compile o auxiliar junto com o projeto de testes.");
        var inicio = new ProcessStartInfo("dotnet")
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            WorkingDirectory = _temporario.FullName,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };
        inicio.Environment.Clear();
        foreach (var nome in new[] { "PATH", "SystemRoot", "WINDIR", "DOTNET_ROOT", "TEMP", "TMP" })
        {
            var valor = Environment.GetEnvironmentVariable(nome);
            if (valor is not null) inicio.Environment[nome] = valor;
        }
        foreach (var argumento in new[] { probe, modo, pasta, ArquivoPayload, contentRoot, proposito })
            inicio.ArgumentList.Add(argumento);

        using var processo = Process.Start(inicio)!;
        var saida = processo.StandardOutput.ReadToEndAsync();
        var erro = processo.StandardError.ReadToEndAsync();
        using var limite = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        try
        {
            await processo.WaitForExitAsync(limite.Token);
        }
        catch (OperationCanceledException)
        {
            processo.Kill(entireProcessTree: true);
            await processo.WaitForExitAsync();
            throw new TimeoutException("O processo da prova da API ultrapassou o limite de diagnóstico.");
        }
        return new ResultadoProcesso(processo.ExitCode, processo.Id, await saida, await erro);
    }

    public void Dispose()
    {
        var raiz = Path.GetFullPath(_temporario.FullName);
        var temp = Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar)
            + Path.DirectorySeparatorChar;
        if (!raiz.StartsWith(temp, StringComparison.OrdinalIgnoreCase)
            || !_temporario.Name.StartsWith("barberapp-api-protection-tests-", StringComparison.Ordinal))
            throw new InvalidOperationException("Recusada limpeza fora da pasta exclusiva da prova da API.");

        Directory.Delete(raiz, recursive: true);
    }

    private sealed record ResultadoProcesso(int Codigo, int ProcessoId, string Saida, string Erro);
}
