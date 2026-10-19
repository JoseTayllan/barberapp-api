using System.IdentityModel.Tokens.Jwt;
using System.Linq.Expressions;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using BarberApp.Domain.Entities;
using BarberApp.Infrastructure.Data;
using BarberApp.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;
using Npgsql;
using Xunit;

namespace BarberApp.IntegrationTests.Payments;

public sealed class MercadoPagoConnectionStatusTests
{
    private const string Endpoint = "/api/integracoes/mercado-pago/status";
    private const string ProtectedPayload = "ONLY-FOR-TESTS-OPAQUE-PROTECTED-PAYLOAD";
    private static readonly DateTimeOffset Now = new(2026, 10, 5, 15, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Status_DeveRetornar401_QuandoApiKeyEstiverAusente()
    {
        using var factory = CriarFactory();
        using var client = CriarClient(factory);
        using var request = CriarRequest();
        request.Headers.Remove("X-API-Key");
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Status_DeveRetornar403_QuandoApiKeyForInvalida()
    {
        using var factory = CriarFactory();
        using var client = CriarClient(factory);
        using var request = CriarRequest();
        request.Headers.Remove("X-API-Key");
        request.Headers.Add("X-API-Key", "ONLY-FOR-TESTS-INVALID-KEY");
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Status_DeveRetornar401_QuandoJwtEstiverAusente()
    {
        using var factory = CriarFactory();
        using var client = CriarClient(factory);
        using var request = CriarRequest(role: null);
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData("Cliente")]
    [InlineData("Barbeiro")]
    public async Task Status_DeveRetornar403_QuandoUsuarioNaoForAdmin(string role)
    {
        using var factory = CriarFactory();
        using var client = CriarClient(factory);
        using var request = CriarRequest(role);
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Status_DeveRetornar401_QuandoAdminNaoTiverIdentificador()
    {
        using var factory = CriarFactory();
        using var client = CriarClient(factory);
        using var request = CriarRequest(admin: null);
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Status_DeveRetornarNaoConectado_QuandoInstalacaoNaoTiverConta()
    {
        using var factory = CriarFactory();
        using var client = CriarClient(factory);
        using var request = CriarRequest();
        using var response = await client.SendAsync(request);
        using var body = await LerSucessoAsync(response);
        var root = body.RootElement;
        Assert.Equal("NaoConectado", root.GetProperty("status").GetString());
        Assert.Equal(JsonValueKind.Null, root.GetProperty("contaId").ValueKind);
        Assert.Equal(JsonValueKind.Null, root.GetProperty("expiraEm").ValueKind);
        Assert.Equal(JsonValueKind.Null, root.GetProperty("sandbox").ValueKind);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Status_DeveRetornarMetadadosSemAbrirTokens_QuandoContaNaoEstiverExpirada(bool sandbox)
    {
        using var factory = CriarFactory();
        using var client = CriarClient(factory);
        await GravarAsync(factory, Now.AddHours(1), sandbox);
        using var request = CriarRequest();
        using var response = await client.SendAsync(request);
        using var body = await LerSucessoAsync(response);
        Assert.Equal("Conectado", body.RootElement.GetProperty("status").GetString());
        ValidarMetadados(body.RootElement, Now.AddHours(1), sandbox);
        using var scope = factory.Services.CreateScope();
        var row = Assert.Single(await scope.ServiceProvider.GetRequiredService<AppDbContext>()
            .ConexoesMercadoPago.AsNoTracking().ToListAsync());
        Assert.Equal(ProtectedPayload, row.TokensProtegidos);
    }

    [Theory]
    [InlineData(-1, "Expirado")]
    [InlineData(0, "Expirado")]
    [InlineData(1, "Conectado")]
    public async Task Status_DeveCompararExpiracaoComRelogioUtc_RespeitandoLimiteExato(int seconds, string status)
    {
        using var factory = CriarFactory();
        using var client = CriarClient(factory);
        var expiresAt = Now.AddSeconds(seconds);
        await GravarAsync(factory, expiresAt);
        using var request = CriarRequest();
        using var response = await client.SendAsync(request);
        using var body = await LerSucessoAsync(response);
        Assert.Equal(status, body.RootElement.GetProperty("status").GetString());
        ValidarMetadados(body.RootElement, expiresAt, true);
    }

    [Fact]
    public async Task Status_DeveConsultarContaDaInstalacao_QuandoOutroAdminEstiverAutenticado()
    {
        using var factory = CriarFactory();
        using var client = CriarClient(factory);
        await GravarAsync(factory, Now.AddHours(1));
        using var request = CriarRequest(admin: "other-admin-status-test");
        using var response = await client.SendAsync(request);
        using var body = await LerSucessoAsync(response);
        Assert.Equal("Conectado", body.RootElement.GetProperty("status").GetString());
        ValidarMetadados(body.RootElement, Now.AddHours(1), true);
    }

    [Fact]
    public async Task Status_DeveRetornar503SemDetalhes_QuandoConsultaAoBancoFalhar()
    {
        using var factory = CriarFactory(falharConsulta: true);
        using var client = CriarClient(factory);
        using var request = CriarRequest();
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.True(response.Headers.CacheControl?.NoStore);
        var content = await response.Content.ReadAsStringAsync();
        using var body = JsonDocument.Parse(content);
        Assert.Equal("MercadoPagoConsultaFalhou", body.RootElement.GetProperty("title").GetString());
        Assert.DoesNotContain("PRIVATE-QUERY-FAILURE-ONLY-FOR-TESTS", content);
        Assert.DoesNotContain(ProtectedPayload, content);
    }

    [Fact]
    public async Task Fixture_DeveSimularFalhaSomenteNaConsultaDeConexao()
    {
        using var factory = CriarFactory(falharConsulta: true);
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await context.Roles.ToListAsync();
        var exception = await Assert.ThrowsAsync<NpgsqlException>(() => context.ConexoesMercadoPago.ToListAsync());
        Assert.Equal("PRIVATE-QUERY-FAILURE-ONLY-FOR-TESTS", exception.Message);
    }

    [Fact]
    public async Task Status_DeveExigirApiKeyEJwtNoOpenApi_SemHerdarExcecaoDoCallback()
    {
        using var factory = CriarFactory(development: true);
        using var client = CriarClient(factory);
        using var response = await client.GetAsync("/swagger/v1/swagger.json");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.True(body.RootElement.GetProperty("paths").TryGetProperty(Endpoint, out var endpoint));
        var requirement = Assert.Single(endpoint.GetProperty("get").GetProperty("security").EnumerateArray());
        Assert.True(requirement.TryGetProperty("ApiKey", out _));
        Assert.True(requirement.TryGetProperty("Bearer", out _));
    }

    private static async Task<JsonDocument> LerSucessoAsync(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.CacheControl?.NoStore);
        var content = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain(ProtectedPayload, content);
        Assert.DoesNotContain("origin-admin-status-test", content);
        var body = JsonDocument.Parse(content);
        Assert.Equal(new[] { "contaId", "expiraEm", "sandbox", "status" },
            body.RootElement.EnumerateObject().Select(property => property.Name).OrderBy(name => name).ToArray());
        return body;
    }

    private static void ValidarMetadados(JsonElement root, DateTimeOffset expiresAt, bool sandbox)
    {
        Assert.Equal(123456L, root.GetProperty("contaId").GetInt64());
        Assert.Equal(expiresAt, root.GetProperty("expiraEm").GetDateTimeOffset());
        Assert.Equal(sandbox, root.GetProperty("sandbox").GetBoolean());
    }

    private static async Task GravarAsync(WebApplicationFactory<BarberApp.API.Controllers.ServicosController> factory,
        DateTimeOffset expiresAt, bool sandbox = true)
    {
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        context.ConexoesMercadoPago.Add(new ConexaoMercadoPago(123456, "origin-admin-status-test",
            ProtectedPayload, expiresAt, sandbox));
        await context.SaveChangesAsync();
    }

    private static WebApplicationFactory<BarberApp.API.Controllers.ServicosController> CriarFactory(
        bool falharConsulta = false, bool development = false) =>
        new BarberAppApiFactory(development ? "Development" : "Testing").WithWebHostBuilder(builder =>
        {
            var databaseName = $"MercadoPagoStatus-{Guid.NewGuid()}";
            // Consultar estado local não depende de configurar ou habilitar OAuth.
            var settings = new Dictionary<string, string?>
            {
                ["MercadoPago:ClientId"] = null,
                ["MercadoPago:ClientSecret"] = null,
                ["MercadoPago:RedirectUri"] = null,
                ["MercadoPago:ConexaoEnabled"] = "false"
            };
            builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(settings));
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<AppDbContext>();
                services.RemoveAll<DbContextOptions<AppDbContext>>();
                services.RemoveAll<IDbContextOptionsConfiguration<AppDbContext>>();
                services.AddDbContext<AppDbContext>(options =>
                {
                    options.UseInMemoryDatabase(databaseName);
                    if (falharConsulta) options.AddInterceptors(new FalhaConsulta());
                });
                services.RemoveAll<TimeProvider>();
                services.AddSingleton<TimeProvider>(new RelogioFixo());
                services.RemoveAll<IHttpClientFactory>();
                services.AddSingleton<IHttpClientFactory>(new ProibirRede());
            });
        });

    private static HttpClient CriarClient(WebApplicationFactory<BarberApp.API.Controllers.ServicosController> factory) =>
        factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

    private static HttpRequestMessage CriarRequest(string? role = "Admin", string? admin = "admin-status-test")
    {
        var request = new HttpRequestMessage(HttpMethod.Get, Endpoint);
        request.Headers.Add("X-API-Key", BarberAppApiFactory.ApiKey);
        if (role is null) return request;
        var claims = new List<Claim> { new(ClaimTypes.Role, role) };
        if (admin is not null) claims.Add(new Claim(ClaimTypes.NameIdentifier, admin));
        var jwt = new JwtSecurityToken(
            issuer: "BarberApp.IntegrationTests", audience: "BarberApp.IntegrationTests", claims: claims,
            expires: DateTime.UtcNow.AddMinutes(5),
            signingCredentials: new SigningCredentials(new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes("integration-tests-secret-key-with-at-least-32-characters")),
                SecurityAlgorithms.HmacSha256));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", new JwtSecurityTokenHandler().WriteToken(jwt));
        return request;
    }

    private sealed class RelogioFixo : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => Now;
    }

    private sealed class ProibirRede : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) =>
            throw new InvalidOperationException("A consulta de status não pode criar cliente HTTP externo.");
    }

    private sealed class FalhaConsulta : IQueryExpressionInterceptor
    {
        public Expression QueryCompilationStarting(Expression queryExpression, QueryExpressionEventData eventData)
        {
            var visitor = new LocalizarConexao();
            visitor.Visit(queryExpression);
            if (visitor.Encontrou)
                throw new NpgsqlException("PRIVATE-QUERY-FAILURE-ONLY-FOR-TESTS");
            return queryExpression;
        }

        private sealed class LocalizarConexao : ExpressionVisitor
        {
            public bool Encontrou { get; private set; }

            protected override Expression VisitExtension(Expression node)
            {
                if (node is EntityQueryRootExpression root)
                {
                    Encontrou |= root.EntityType.ClrType == typeof(ConexaoMercadoPago);
                    return node;
                }
                return base.VisitExtension(node);
            }
        }
    }
}
