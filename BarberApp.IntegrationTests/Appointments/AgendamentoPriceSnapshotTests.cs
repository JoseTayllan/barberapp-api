using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using BarberApp.Domain.Entities;
using BarberApp.Domain.Enums;
using BarberApp.Infrastructure.Data;
using BarberApp.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace BarberApp.IntegrationTests.Appointments;

public sealed class AgendamentoPriceSnapshotTests
{
    private const string Endpoint = "/api/agendamentos";
    private static readonly DateTime BookingTime = new(2030, 1, 7, 10, 0, 0, DateTimeKind.Unspecified);

    [Theory]
    [InlineData(4000)]
    [InlineData(4099)]
    [InlineData(10101)]
    public async Task Criar_DeveRegistrarPrecoDoServidorSemPerderCentavos_QuandoClienteAgendar(int cents)
    {
        using var scenario = new Cenario();
        await scenario.PrepararAsync(cents / 100m);
        using var response = await scenario.CriarAsync();
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
        using var created = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        ValidarPreco(created.RootElement, cents / 100m);
        Assert.Equal("Pendente", created.RootElement.GetProperty("status").GetString());
        var id = created.RootElement.GetProperty("id").GetGuid();
        using var saved = await scenario.ObterAsync(id);
        ValidarPreco(saved.RootElement, cents / 100m);
        ValidarCamposAnteriores(saved.RootElement, cents / 100m);
    }

    [Theory]
    [InlineData(5000)]
    [InlineData(3000)]
    public async Task Consultar_DevePreservarPrecoCombinado_EAplicarPrecoNovoSomenteANovaReserva(int newCents)
    {
        using var scenario = new Cenario();
        await scenario.PrepararAsync(40m);
        var original = await scenario.CriarIdAsync();
        await scenario.AlterarPrecoAsync(newCents / 100m);
        using var oldBooking = await scenario.ObterAsync(original);
        ValidarPreco(oldBooking.RootElement, 40m);
        // Campo legado conserva o significado: preço atual no cadastro do serviço.
        ValidarCamposAnteriores(oldBooking.RootElement, newCents / 100m);
        var next = await scenario.CriarIdAsync(hour: 11);
        using var newBooking = await scenario.ObterAsync(next);
        ValidarPreco(newBooking.RootElement, newCents / 100m);
        using var reread = await scenario.ObterAsync(original);
        ValidarPreco(reread.RootElement, 40m);
    }

    [Fact]
    public async Task Criar_DeveIgnorarPrecoForjadoNoBody_EUsarValorDoServidor()
    {
        using var scenario = new Cenario();
        await scenario.PrepararAsync(40m);
        using var response = await scenario.CriarAsync(forgedPrice: true);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        using var created = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        ValidarPreco(created.RootElement, 40m);
        using var saved = await scenario.ObterAsync(created.RootElement.GetProperty("id").GetGuid());
        ValidarPreco(saved.RootElement, 40m);
    }

    [Fact]
    public async Task Listar_DeveRetornarPrecoCombinadoSemAlterarCamposLegados_AposAtualizacaoDoServico()
    {
        using var scenario = new Cenario();
        await scenario.PrepararAsync(40m);
        var id = await scenario.CriarIdAsync();
        await scenario.AlterarPrecoAsync(50m);
        using var request = scenario.Request(HttpMethod.Get, Endpoint);
        using var response = await scenario.Client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var list = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var row = Assert.Single(list.RootElement.EnumerateArray());
        Assert.Equal(id, row.GetProperty("id").GetGuid());
        ValidarPreco(row, 40m);
        ValidarCamposAnteriores(row, 50m);
    }

    [Theory]
    [InlineData("confirmar", "Confirmado")]
    [InlineData("cancelar", "Cancelado")]
    public async Task AlterarStatus_DevePreservarPrecoCombinado_QuandoServicoMudar(string action, string status)
    {
        using var scenario = new Cenario();
        await scenario.PrepararAsync(40m);
        var id = await scenario.CriarIdAsync();
        await scenario.AlterarPrecoAsync(50m);
        using var request = scenario.Request(HttpMethod.Patch, $"{Endpoint}/{id}/{action}", role: "Admin");
        using var response = await scenario.Client.SendAsync(request);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        using var saved = await scenario.ObterAsync(id);
        Assert.Equal(status, saved.RootElement.GetProperty("status").GetString());
        ValidarPreco(saved.RootElement, 40m);
    }

    [Fact]
    public async Task PagamentoMock_DeveCobrarPrecoAgendadoSemMudarContrato_QuandoCadastroDoServicoMudar()
    {
        using var scenario = new Cenario();
        await scenario.PrepararAsync(40m);
        var id = await scenario.CriarIdAsync();
        await scenario.AlterarPrecoAsync(50m);
        using var request = scenario.Request(HttpMethod.Post, $"/api/pagamentos/{id}");
        using var response = await scenario.Client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(40m, json.RootElement.GetProperty("valor").GetDecimal());
        Assert.Equal("Mock", json.RootElement.GetProperty("gateway").GetString());
        Assert.Equal("Aprovado", json.RootElement.GetProperty("status").GetString());
        Assert.StartsWith("MOCK-", json.RootElement.GetProperty("gatewayTransacaoId").GetString());
        Assert.Equal(new[] { "gateway", "gatewayTransacaoId", "id", "status", "valor" },
            json.RootElement.EnumerateObject().Select(property => property.Name).OrderBy(name => name).ToArray());
        using var scope = scenario.Factory.Services.CreateScope();
        var payment = Assert.Single(await scope.ServiceProvider.GetRequiredService<AppDbContext>()
            .Pagamentos.AsNoTracking().ToListAsync());
        Assert.Equal(40m, payment.Valor);
    }

    [Fact]
    public async Task Consultar_DeveRepresentarPrecoHistoricoDesconhecidoComoNulo_QuandoRegistroForLegado()
    {
        using var scenario = new Cenario();
        await scenario.PrepararAsync(40m);
        Guid id;
        using (var scope = scenario.Factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var legacy = new Agendamento(scenario.Cliente.Id, scenario.Barbeiro.Id, scenario.Servico.Id,
                new DateTime(2030, 1, 7, 13, 0, 0, DateTimeKind.Utc));
            context.Agendamentos.Add(legacy);
            await context.SaveChangesAsync();
            id = legacy.Id;
        }
        await scenario.AlterarPrecoAsync(50m);
        using var saved = await scenario.ObterAsync(id);
        Assert.True(saved.RootElement.TryGetProperty("precoAgendado", out var price),
            "Registro legado deve expor preço combinado desconhecido, sem inventar histórico.");
        Assert.Equal(JsonValueKind.Null, price.ValueKind);
        ValidarCamposAnteriores(saved.RootElement, 50m);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-100)]
    public async Task Criar_DeveRecusarPrecoNaoPositivoSemSalvarReserva_QuandoCadastroEstiverInvalido(int cents)
    {
        using var scenario = new Cenario();
        await scenario.PrepararAsync(cents / 100m);
        using var response = await scenario.CriarAsync();
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var scope = scenario.Factory.Services.CreateScope();
        Assert.Empty(await scope.ServiceProvider.GetRequiredService<AppDbContext>().Agendamentos.ToListAsync());
    }

    [Theory]
    [InlineData("missing-key", 401)]
    [InlineData("invalid-key", 403)]
    [InlineData("missing-jwt", 401)]
    public async Task Criar_DevePreservarAutenticacaoExistente_SemSalvarReserva(string identity, int status)
    {
        using var scenario = new Cenario();
        await scenario.PrepararAsync(40m);
        using var response = await scenario.CriarAsync(identity: identity);
        Assert.Equal((HttpStatusCode)status, response.StatusCode);
        using var scope = scenario.Factory.Services.CreateScope();
        Assert.Empty(await scope.ServiceProvider.GetRequiredService<AppDbContext>().Agendamentos.ToListAsync());
    }

    private static void ValidarPreco(JsonElement row, decimal price)
    {
        Assert.True(row.TryGetProperty("precoAgendado", out var snapshot),
            "Resposta deve expor o preço combinado, independente do preço atual do serviço.");
        Assert.Equal(price, snapshot.GetDecimal());
    }

    private static void ValidarCamposAnteriores(JsonElement row, decimal currentPrice)
    {
        Assert.NotEqual(Guid.Empty, row.GetProperty("id").GetGuid());
        Assert.Equal("Cliente de teste", row.GetProperty("nomeCliente").GetString());
        Assert.Equal("Barbeiro de teste", row.GetProperty("nomeBarbeiro").GetString());
        Assert.Equal("Corte de teste", row.GetProperty("nomeServico").GetString());
        Assert.Equal(currentPrice, row.GetProperty("precoServico").GetDecimal());
        Assert.Equal(DateTimeKind.Utc, row.GetProperty("dataHora").GetDateTime().Kind);
        Assert.False(string.IsNullOrWhiteSpace(row.GetProperty("status").GetString()));
    }

    private sealed class Cenario : IDisposable
    {
        public WebApplicationFactory<BarberApp.API.Controllers.ServicosController> Factory { get; }
        public HttpClient Client { get; }
        public Cliente Cliente { get; } = new("Cliente de teste", "cliente@example.test", "11999990000");
        public Barbeiro Barbeiro { get; } = new("Barbeiro de teste", "11999990001", null);
        public Servico Servico { get; private set; } = null!;

        public Cenario()
        {
            var database = $"AgendamentoPriceSnapshot-{Guid.NewGuid()}";
            Factory = new BarberAppApiFactory().WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<AppDbContext>();
                services.RemoveAll<DbContextOptions<AppDbContext>>();
                services.RemoveAll<IDbContextOptionsConfiguration<AppDbContext>>();
                services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase(database));
                services.RemoveAll<IHttpClientFactory>();
                services.AddSingleton<IHttpClientFactory>(new ProvedorProibido());
            }));
            Client = Factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        }

        public async Task PrepararAsync(decimal price)
        {
            Servico = new Servico("Corte de teste", price, 30);
            using var scope = Factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            context.Clientes.Add(Cliente);
            context.Barbeiros.Add(Barbeiro);
            context.Servicos.Add(Servico);
            context.DisponibilidadesBarbeiros.Add(new DisponibilidadeBarbeiro(Barbeiro.Id, DiaSemana.Segunda,
                TimeSpan.FromHours(8), TimeSpan.FromHours(18)));
            await context.SaveChangesAsync();
        }

        public Task<HttpResponseMessage> CriarAsync(int hour = 10, bool forgedPrice = false, string identity = "Cliente")
        {
            var request = Request(HttpMethod.Post, Endpoint, identity: identity);
            request.Content = forgedPrice ? JsonContent.Create(new
            {
                barbeiroId = Barbeiro.Id,
                servicoId = Servico.Id,
                dataHora = BookingTime.AddHours(hour - 10),
                observacao = "ONLY-FOR-TESTS-BOOKING",
                precoAgendado = 0.01m,
                precoServico = 0.01m
            }) : JsonContent.Create(new
            {
                barbeiroId = Barbeiro.Id,
                servicoId = Servico.Id,
                dataHora = BookingTime.AddHours(hour - 10),
                observacao = "ONLY-FOR-TESTS-BOOKING"
            });
            return EnviarEDescartarAsync(request);
        }

        private async Task<HttpResponseMessage> EnviarEDescartarAsync(HttpRequestMessage request)
        {
            using (request) return await Client.SendAsync(request);
        }

        public async Task<Guid> CriarIdAsync(int hour = 10)
        {
            using var response = await CriarAsync(hour);
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            return json.RootElement.GetProperty("id").GetGuid();
        }

        public async Task<JsonDocument> ObterAsync(Guid id)
        {
            using var request = Request(HttpMethod.Get, $"{Endpoint}/{id}");
            using var response = await Client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            return JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        }

        public async Task AlterarPrecoAsync(decimal price)
        {
            using var scope = Factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var service = await context.Servicos.SingleAsync();
            service.Atualizar(service.Nome, price, service.DuracaoMinuto, service.Descricao);
            await context.SaveChangesAsync();
        }

        public HttpRequestMessage Request(HttpMethod method, string path, string role = "Cliente", string identity = "Cliente")
        {
            var request = new HttpRequestMessage(method, path);
            if (identity != "missing-key") request.Headers.Add("X-API-Key",
                identity == "invalid-key" ? "ONLY-FOR-TESTS-WRONG-KEY" : BarberAppApiFactory.ApiKey);
            if (identity == "missing-jwt") return request;
            var jwt = new JwtSecurityToken(issuer: "BarberApp.IntegrationTests", audience: "BarberApp.IntegrationTests",
                claims: new[] { new Claim(ClaimTypes.Role, role), new Claim(ClaimTypes.Email, Cliente.Email),
                    new Claim(ClaimTypes.NameIdentifier, "ONLY-FOR-TESTS-CLIENT-ID") },
                expires: DateTime.UtcNow.AddMinutes(5),
                signingCredentials: new SigningCredentials(new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes("integration-tests-secret-key-with-at-least-32-characters")), SecurityAlgorithms.HmacSha256));
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", new JwtSecurityTokenHandler().WriteToken(jwt));
            return request;
        }

        public void Dispose()
        {
            Client.Dispose();
            Factory.Dispose();
        }

        private sealed class ProvedorProibido : IHttpClientFactory
        {
            public HttpClient CreateClient(string name) =>
                throw new InvalidOperationException("ONLY-FOR-TESTS-NO-REMOTE-CALL-ALLOWED");
        }
    }
}
