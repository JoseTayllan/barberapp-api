using System.Diagnostics;
using System.Text;
using Xunit;

namespace BarberApp.IntegrationTests.Security;

// Qualificação do componente .NET com keyring isolado, não do composition root da API.
public sealed class DataProtectionProcessTests : IDisposable
{
    private const string Aplicacao = "BarberApp.ONLY-FOR-TESTS";
    private const string Proposito = "BarberApp.MercadoPago.Tokens.v1";
    private readonly DirectoryInfo _temporario = Directory.CreateTempSubdirectory("barberapp-protection-tests-");

    [Fact]
    public async Task Recuperar_DeveLerTextoFicticio_EmOutroProcessoComMesmasChaves()
    {
        await ProtegerAsync();
        Assert.NotEmpty(Directory.GetFiles(PastaChaves, "key-*.xml"));
        var payload = await File.ReadAllTextAsync(ArquivoPayload);
        Assert.DoesNotContain("ONLY-FOR-TESTS-NOT-A-MERCADO-PAGO-TOKEN", payload);
        await ValidarRecuperacaoAsync(PastaChaves, Aplicacao, Proposito, aceita: true);
    }

    [Fact]
    public async Task Recuperar_DeveRejeitarTexto_QuandoPropositoForDiferente()
    {
        await ProtegerAsync();
        await ValidarRecuperacaoAsync(PastaChaves, Aplicacao, "ONLY-FOR-TESTS-WRONG-PURPOSE", aceita: false);
    }

    [Fact]
    public async Task Recuperar_DeveRejeitarTexto_QuandoAplicacaoForDiferente()
    {
        await ProtegerAsync();
        await ValidarRecuperacaoAsync(PastaChaves, "OTHER-APPLICATION-ONLY-FOR-TESTS", Proposito, aceita: false);
    }

    [Fact]
    public async Task Recuperar_DeveRejeitarTexto_QuandoPastaNaoContiverAsChaves()
    {
        await ProtegerAsync();
        var pastaVazia = Directory.CreateDirectory(Path.Combine(_temporario.FullName, "sem-chaves")).FullName;
        await ValidarRecuperacaoAsync(pastaVazia, Aplicacao, Proposito, aceita: false);
        Assert.Empty(Directory.GetFiles(pastaVazia, "key-*.xml"));
    }

    [Fact]
    public async Task Recuperar_DeveLerTexto_UsandoCopiaDasChavesNoMesmoContextoDoUsuario()
    {
        await ProtegerAsync();
        var copia = Directory.CreateDirectory(Path.Combine(_temporario.FullName, "copia-chaves")).FullName;
        foreach (var arquivo in Directory.GetFiles(PastaChaves, "key-*.xml"))
            File.Copy(arquivo, Path.Combine(copia, Path.GetFileName(arquivo)));

        await ValidarRecuperacaoAsync(copia, Aplicacao, Proposito, aceita: true);
    }

    [Fact]
    public async Task Recuperar_DeveRejeitarTexto_QuandoCiphertextForAdulterado()
    {
        await ProtegerAsync();
        var payload = await File.ReadAllTextAsync(ArquivoPayload);
        // Mudar um byte autenticado sem depender de representação textual inválida.
        var bytes = Convert.FromBase64String(payload.Replace('-', '+').Replace('_', '/')
            .PadRight((payload.Length + 3) / 4 * 4, '='));
        bytes[^1] ^= 1;
        await File.WriteAllTextAsync(ArquivoPayload,
            Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_'));
        await ValidarRecuperacaoAsync(PastaChaves, Aplicacao, Proposito, aceita: false);
    }

    private string PastaChaves => Path.Combine(_temporario.FullName, "chaves");
    private string ArquivoPayload => Path.Combine(_temporario.FullName, "payload-protegido.txt");

    private async Task ProtegerAsync()
    {
        Directory.CreateDirectory(PastaChaves);
        var resultado = await ExecutarAsync("proteger", PastaChaves, Aplicacao, Proposito);
        Assert.Equal(0, resultado.Codigo);
        Assert.Equal("PROTEGIDO", resultado.Saida.Trim());
        Assert.Empty(resultado.Erro);
    }

    private async Task ValidarRecuperacaoAsync(string pasta, string aplicacao, string proposito, bool aceita)
    {
        var resultado = await ExecutarAsync("recuperar", pasta, aplicacao, proposito);
        Assert.Equal(aceita ? 0 : 3, resultado.Codigo);
        Assert.Equal(aceita ? "RECUPERADO" : "REJEITADO", resultado.Saida.Trim());
        Assert.Empty(resultado.Erro);
    }

    private async Task<(int Codigo, string Saida, string Erro)> ExecutarAsync(
        string modo, string pasta, string aplicacao, string proposito)
    {
        var probe = Path.Combine(AppContext.BaseDirectory, "ProtectionProbe",
            "BarberApp.TestSupport.DataProtectionProbe.dll");
        Assert.True(File.Exists(probe), "O executável de apoio deve ser compilado junto dos testes.");
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
        // O filho não herda credenciais da sessão nem carrega arquivos de configuração.
        var permitidas = new[] { "PATH", "SystemRoot", "WINDIR", "DOTNET_ROOT", "TEMP", "TMP" };
        inicio.Environment.Clear();
        foreach (var nome in permitidas)
        {
            var valor = Environment.GetEnvironmentVariable(nome);
            if (valor is not null) inicio.Environment[nome] = valor;
        }
        foreach (var argumento in new[] { probe, modo, pasta, ArquivoPayload, aplicacao, proposito })
            inicio.ArgumentList.Add(argumento);

        using var processo = Process.Start(inicio)!;
        var saida = processo.StandardOutput.ReadToEndAsync();
        var erro = processo.StandardError.ReadToEndAsync();
        using var limite = new CancellationTokenSource(TimeSpan.FromSeconds(20));
        try
        {
            await processo.WaitForExitAsync(limite.Token);
        }
        catch (OperationCanceledException)
        {
            processo.Kill(entireProcessTree: true);
            await processo.WaitForExitAsync();
            throw new TimeoutException("O processo de teste ultrapassou o limite de diagnóstico.");
        }
        return (processo.ExitCode, await saida, await erro);
    }

    public void Dispose()
    {
        var raiz = Path.GetFullPath(_temporario.FullName);
        var temp = Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar)
            + Path.DirectorySeparatorChar;
        if (!raiz.StartsWith(temp, StringComparison.OrdinalIgnoreCase)
            || !_temporario.Name.StartsWith("barberapp-protection-tests-", StringComparison.Ordinal))
            throw new InvalidOperationException("Recusada limpeza fora da pasta temporária exclusiva do teste.");

        Directory.Delete(raiz, recursive: true);
    }
}
