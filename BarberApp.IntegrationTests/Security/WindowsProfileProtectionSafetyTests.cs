using System.Diagnostics;
using System.Text.Json;
using Xunit;

namespace BarberApp.IntegrationTests.Security;

// Somente barreiras/inspeção de opções: nunca executa Protect/Unprotect no perfil real.
public sealed class WindowsProfileProtectionSafetyTests : IDisposable
{
    private const string Confirmacao = "--confirmar-leitura-do-perfil-windows";
    private const string Proposito = "BarberApp.MercadoPago.Tokens.v1";
    private readonly DirectoryInfo _temporario = Directory.CreateTempSubdirectory("barberapp-profile-probe-");

    [Fact]
    public async Task Perfil_DeveInspecionarOpcoesSemGerarChaves_OuSobrescreverOsPadroesDoProvider()
    {
        var resultado = await ExecutarAsync("perfil-inspecionar", Confirmacao, ArquivoPayload, ContentRoot, Proposito);
        Assert.True(resultado.Codigo == 0, $"Inspeção não implementada ou indisponível: código {resultado.Codigo}.");
        Assert.Empty(resultado.Erro);
        using var documento = JsonDocument.Parse(resultado.Saida);
        var json = documento.RootElement;
        Assert.Equal(new[] { "criptografiaExplicita", "geracaoAutomatica", "identidadeHash", "origem", "repositorioExplicito", "status" },
            json.EnumerateObject().Select(property => property.Name).OrderBy(name => name).ToArray());
        Assert.Equal("Inspecionado", json.GetProperty("status").GetString());
        Assert.Equal("ApiServices", json.GetProperty("origem").GetString());
        Assert.False(json.GetProperty("geracaoAutomatica").GetBoolean());
        Assert.False(json.GetProperty("repositorioExplicito").GetBoolean());
        Assert.False(json.GetProperty("criptografiaExplicita").GetBoolean());
        Assert.Matches("^[A-F0-9]{64}$", json.GetProperty("identidadeHash").GetString()!);
        Assert.False(File.Exists(ArquivoPayload));
        Assert.DoesNotContain("ONLY-FOR-TESTS-NOT-A-MERCADO-PAGO-TOKEN", resultado.Saida);
    }

    [Theory]
    [InlineData("perfil-inspecionar")]
    [InlineData("perfil-proteger")]
    [InlineData("perfil-recuperar")]
    public async Task Perfil_DeveRecusarSemConfirmacao_AntesDeInicializarOuEscrever(string modo)
    {
        var resultado = await ExecutarAsync(modo, "SEM-CONFIRMACAO", ArquivoPayload, ContentRoot, Proposito);
        ValidarRecusa(resultado, "ConfirmacaoNecessaria");
        Assert.False(File.Exists(ArquivoPayload));
    }

    [Theory]
    [InlineData("perfil-proteger")]
    [InlineData("perfil-recuperar")]
    public async Task Perfil_DeveRecusarPropositoArbitrario_AntesDeQualquerOperacao(string modo)
    {
        var resultado = await ExecutarAsync(modo, Confirmacao, ArquivoPayload, ContentRoot,
            "ONLY-FOR-TESTS-WRONG-PURPOSE");
        ValidarRecusa(resultado, "PropositoInvalido");
        Assert.False(File.Exists(ArquivoPayload));
    }

    [Theory]
    [InlineData("perfil-proteger")]
    [InlineData("perfil-recuperar")]
    public async Task Perfil_DeveRecusarDestinoForaDaPastaTemporariaExclusiva(string modo)
    {
        var fora = Directory.CreateDirectory(Path.Combine(_temporario.FullName, "destino-nao-autorizado"));
        var destino = Path.Combine(fora.FullName, "payload-protegido.txt");
        var resultado = await ExecutarAsync(modo, Confirmacao, destino, ContentRoot, Proposito);
        ValidarRecusa(resultado, "DestinoInvalido");
        Assert.False(File.Exists(destino));
    }

    [Fact]
    public async Task Perfil_DeveRecusarContentRootInexistente_SemCriarDiretorioOuPayload()
    {
        var inexistente = Path.Combine(_temporario.FullName, "api-inexistente");
        var resultado = await ExecutarAsync("perfil-inspecionar", Confirmacao, ArquivoPayload, inexistente, Proposito);
        ValidarRecusa(resultado, "ContextoInvalido");
        Assert.False(Directory.Exists(inexistente));
        Assert.False(File.Exists(ArquivoPayload));
    }

    [Fact]
    public async Task Perfil_DeveRecusarProtecao_SemSobrescreverArquivoExistente()
    {
        const string sentinela = "ONLY-FOR-TESTS-PRESERVE-THIS-FILE";
        await File.WriteAllTextAsync(ArquivoPayload, sentinela);
        var resultado = await ExecutarAsync("perfil-proteger", Confirmacao, ArquivoPayload, ContentRoot, Proposito);
        ValidarRecusa(resultado, "DestinoJaExiste");
        Assert.Equal(sentinela, await File.ReadAllTextAsync(ArquivoPayload));
        Assert.DoesNotContain(sentinela, resultado.Saida);
    }

    [Fact]
    public async Task Perfil_DeveRecusarRecuperacao_QuandoPayloadNaoExistir()
    {
        var resultado = await ExecutarAsync("perfil-recuperar", Confirmacao, ArquivoPayload, ContentRoot, Proposito);
        ValidarRecusa(resultado, "PayloadAusente");
        Assert.False(File.Exists(ArquivoPayload));
    }

    private string ArquivoPayload => Path.Combine(_temporario.FullName, "payload-protegido.txt");
    private string ContentRoot => Path.Combine(_temporario.FullName, "api");

    private static void ValidarRecusa(ResultadoProcesso resultado, string status)
    {
        Assert.True(resultado.Codigo == 6,
            $"Esperada recusa explícita de segurança (6), recebido {resultado.Codigo}. Modo ausente (2) não comprova a barreira.");
        Assert.Empty(resultado.Erro);
        using var documento = JsonDocument.Parse(resultado.Saida);
        var campo = Assert.Single(documento.RootElement.EnumerateObject());
        Assert.Equal("status", campo.Name);
        Assert.Equal(status, campo.Value.GetString());
    }

    private async Task<ResultadoProcesso> ExecutarAsync(params string[] argumentos)
    {
        Directory.CreateDirectory(ContentRoot);
        var probe = Path.Combine(AppContext.BaseDirectory, "ProtectionProbe",
            "BarberApp.TestSupport.DataProtectionProbe.dll");
        Assert.True(File.Exists(probe), "Compile o auxiliar junto com os testes.");
        var inicio = new ProcessStartInfo("dotnet")
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            WorkingDirectory = _temporario.FullName
        };
        inicio.Environment.Clear();
        foreach (var nome in new[] { "PATH", "SystemRoot", "WINDIR", "DOTNET_ROOT", "TEMP", "TMP" })
        {
            var valor = Environment.GetEnvironmentVariable(nome);
            if (valor is not null) inicio.Environment[nome] = valor;
        }
        inicio.ArgumentList.Add(probe);
        foreach (var argumento in argumentos) inicio.ArgumentList.Add(argumento);
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
            throw new TimeoutException("A verificação das barreiras excedeu o limite de diagnóstico.");
        }
        return new ResultadoProcesso(processo.ExitCode, await saida, await erro);
    }

    public void Dispose()
    {
        var raiz = Path.GetFullPath(_temporario.FullName);
        var temp = Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar)
            + Path.DirectorySeparatorChar;
        if (!raiz.StartsWith(temp, StringComparison.OrdinalIgnoreCase)
            || !_temporario.Name.StartsWith("barberapp-profile-probe-", StringComparison.Ordinal))
            throw new InvalidOperationException("Recusada limpeza fora da pasta exclusiva da especificação.");
        Directory.Delete(raiz, recursive: true);
    }

    private sealed record ResultadoProcesso(int Codigo, string Saida, string Erro);
}
