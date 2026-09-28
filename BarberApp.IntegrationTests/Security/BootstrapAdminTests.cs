using System.Text.RegularExpressions;
using BarberApp.Infrastructure.Data;
using BarberApp.Infrastructure.Identity;
using BarberApp.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace BarberApp.IntegrationTests.Security;

public sealed class BootstrapAdminTests
{
    private const string AdminName = "Administrador de Teste";
    private const string AdminEmail = "bootstrap-admin@barberapp.test";

    [Fact]
    public void Program_NaoDevePassarSenhaLiteralParaCriacaoDoAdmin()
    {
        var programContent = File.ReadAllText(GetProgramPath());
        var createUserWithLiteralPassword = new Regex(
            @"CreateAsync\s*\(\s*[^,]+,\s*\""[^\""\r\n]+\""\s*\)",
            RegexOptions.CultureInvariant);

        Assert.DoesNotMatch(createUserWithLiteralPassword, programContent);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("false")]
    public async Task Startup_NaoDeveCriarAdminBootstrap_QuandoEstiverAusenteOuDesabilitado(
        string? enabled)
    {
        var configuration = new Dictionary<string, string?>();
        if (enabled is not null)
        {
            configuration["BootstrapAdmin:Enabled"] = enabled;
        }

        using var factory = new BootstrapAdminApiFactory(configuration);
        using var client = factory.CreateClient();
        using var scope = factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        Assert.Empty(userManager.Users);
        Assert.True(await roleManager.RoleExistsAsync("Admin"));
        Assert.True(await roleManager.RoleExistsAsync("Barbeiro"));
        Assert.True(await roleManager.RoleExistsAsync("Cliente"));
    }

    [Fact]
    public async Task Startup_DeveCriarAdminConfiguradoComRoleAdmin_QuandoEstiverHabilitado()
    {
        using var factory = new BootstrapAdminApiFactory(CreateValidConfiguration());
        using var client = factory.CreateClient();
        using var scope = factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var admin = await userManager.FindByEmailAsync(AdminEmail);

        Assert.NotNull(admin);
        Assert.Equal(AdminName, admin.NomeCompleto);
        Assert.Equal(AdminEmail, admin.UserName);
        Assert.True(await userManager.IsInRoleAsync(admin, "Admin"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("sim")]
    [InlineData("1")]
    public void Startup_DeveFalharSemExporValor_QuandoEnabledForInvalido(string invalidValue)
    {
        var configuration = CreateValidConfiguration();
        configuration["BootstrapAdmin:Enabled"] = invalidValue;
        using var factory = new BootstrapAdminApiFactory(configuration);

        var exception = Assert.ThrowsAny<Exception>(() => factory.CreateClient());
        var exceptionMessages = GetExceptionMessages(exception);

        Assert.Contains(
            "BootstrapAdmin:Enabled",
            exceptionMessages,
            StringComparison.Ordinal);

        if (!string.IsNullOrWhiteSpace(invalidValue))
        {
            Assert.DoesNotContain(invalidValue, exceptionMessages, StringComparison.Ordinal);
        }
    }

    [Theory]
    [InlineData("NomeCompleto", null)]
    [InlineData("NomeCompleto", "   ")]
    [InlineData("Email", null)]
    [InlineData("Email", "   ")]
    [InlineData("Password", null)]
    [InlineData("Password", "   ")]
    public void Startup_DeveFalharSemRevelarSegredo_QuandoCampoObrigatorioEstiverAusenteOuVazio(
        string field,
        string? invalidValue)
    {
        var configuration = CreateValidConfiguration();
        var settingName = $"BootstrapAdmin:{field}";

        if (invalidValue is null)
        {
            configuration.Remove(settingName);
        }
        else
        {
            configuration[settingName] = invalidValue;
        }

        using var factory = new BootstrapAdminApiFactory(configuration);

        var exception = Assert.ThrowsAny<Exception>(() => factory.CreateClient());
        var exceptionMessages = GetExceptionMessages(exception);

        Assert.Contains(settingName, exceptionMessages, StringComparison.Ordinal);
        Assert.DoesNotContain(CreateTestPassword(), exceptionMessages, StringComparison.Ordinal);
    }

    [Fact]
    public void Startup_DeveFalharSemPromover_QuandoUsuarioConfiguradoExistirSemRoleAdmin()
    {
        var existingUserId = Guid.NewGuid().ToString();
        using var factory = new BootstrapAdminApiFactory(
            CreateValidConfiguration(),
            dbContext => SeedExistingUser(dbContext, existingUserId));

        Assert.ThrowsAny<Exception>(() => factory.CreateClient());

        using var dbContext = factory.CreateDbContext();
        var existingUser = Assert.Single(dbContext.Users);
        Assert.Equal(existingUserId, existingUser.Id);
        Assert.Empty(dbContext.UserRoles);
    }

    [Fact]
    public async Task Startup_NaoDeveDuplicarUsuario_QuandoAdminConfiguradoJaExistirComRoleAdmin()
    {
        var existingUserId = Guid.NewGuid().ToString();
        using var factory = new BootstrapAdminApiFactory(
            CreateValidConfiguration(),
            dbContext => SeedExistingAdmin(dbContext, existingUserId));
        using var client = factory.CreateClient();
        using var scope = factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var users = userManager.Users.ToList();

        var existingAdmin = Assert.Single(users);
        Assert.Equal(existingUserId, existingAdmin.Id);
        Assert.Equal(AdminEmail, existingAdmin.Email);
        Assert.True(await userManager.IsInRoleAsync(existingAdmin, "Admin"));
    }

    [Fact]
    public void Startup_DeveFalharSomenteComCodigosIdentity_QuandoPasswordForRejeitada()
    {
        const string invalidPassword = "weak";
        var configuration = CreateValidConfiguration();
        configuration["BootstrapAdmin:Password"] = invalidPassword;
        using var factory = new BootstrapAdminApiFactory(configuration);

        var exception = Assert.ThrowsAny<Exception>(() => factory.CreateClient());
        var failureMessage = GetInnermostExceptionMessage(exception);

        Assert.Contains("PasswordTooShort", failureMessage, StringComparison.Ordinal);
        Assert.Contains("PasswordRequiresDigit", failureMessage, StringComparison.Ordinal);
        Assert.DoesNotContain(invalidPassword, failureMessage, StringComparison.Ordinal);
        Assert.Matches(
            @"^\s*(PasswordTooShort|PasswordRequiresDigit)(\s*[,;]\s*(PasswordTooShort|PasswordRequiresDigit))*\s*$",
            failureMessage);
    }

    [Fact]
    public async Task Startup_DeveSerIdempotente_EmDuasInicializacoesSequenciais()
    {
        var databaseName = $"BootstrapAdminIdempotency-{Guid.NewGuid()}";
        var databaseRoot = new InMemoryDatabaseRoot();

        using (var firstFactory = new BootstrapAdminApiFactory(
            CreateValidConfiguration(),
            databaseName: databaseName,
            databaseRoot: databaseRoot))
        using (var firstClient = firstFactory.CreateClient())
        {
        }

        using var secondFactory = new BootstrapAdminApiFactory(
            CreateValidConfiguration(),
            databaseName: databaseName,
            databaseRoot: databaseRoot);
        using var secondClient = secondFactory.CreateClient();
        using var scope = secondFactory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var admin = Assert.Single(userManager.Users);
        Assert.Equal(AdminEmail, admin.Email);
        Assert.True(await userManager.IsInRoleAsync(admin, "Admin"));
    }

    [Fact]
    public void Startup_DeveFalhar_QuandoCriacaoDeRoleForRejeitada()
    {
        using var factory = new BootstrapAdminApiFactory(
            new Dictionary<string, string?>
            {
                ["BootstrapAdmin:Enabled"] = "false"
            },
            configureServices: services =>
                services.Replace(ServiceDescriptor.Scoped<IRoleStore<IdentityRole>, RejectingRoleStore>()));

        var exception = Assert.ThrowsAny<Exception>(() => factory.CreateClient());
        var failureMessage = GetInnermostExceptionMessage(exception);

        Assert.Equal("RoleCreationRejected", failureMessage);
        Assert.DoesNotContain("Falha controlada pelo teste.", failureMessage, StringComparison.Ordinal);
        Assert.DoesNotContain(BarberAppApiFactory.ApiKey, failureMessage, StringComparison.Ordinal);
    }

    [Fact]
    public void Startup_NaoDeveDeixarUsuarioOrfao_QuandoAddToRoleFalharECleanupPassar()
    {
        var state = new RejectingAddToRoleState(cleanupSucceeds: true);
        using var factory = new BootstrapAdminApiFactory(
            CreateValidConfiguration(),
            configureServices: services => ReplaceUserManager(
                services,
                new RejectingAddToRoleUserManager(state)));

        var exception = Assert.ThrowsAny<Exception>(() => factory.CreateClient());
        var exceptionMessages = GetExceptionMessages(exception);

        Assert.True(state.DeleteWasCalled);
        Assert.Null(state.User);
        Assert.Equal("AddToRoleRejected", GetInnermostExceptionMessage(exception));
        Assert.DoesNotContain("Descrição controlada de role.", exceptionMessages, StringComparison.Ordinal);
        Assert.DoesNotContain("Descrição controlada de limpeza.", exceptionMessages, StringComparison.Ordinal);
        Assert.DoesNotContain(CreateTestPassword(), exceptionMessages, StringComparison.Ordinal);
    }

    [Fact]
    public void Startup_DeveManterUsuarioERelatarAmbosCodigos_QuandoAddToRoleECleanupFalharem()
    {
        var state = new RejectingAddToRoleState(cleanupSucceeds: false);
        using var factory = new BootstrapAdminApiFactory(
            CreateValidConfiguration(),
            configureServices: services => ReplaceUserManager(
                services,
                new RejectingAddToRoleUserManager(state)));

        var exception = Assert.ThrowsAny<Exception>(() => factory.CreateClient());
        var exceptionMessages = GetExceptionMessages(exception);

        Assert.True(state.DeleteWasCalled);
        Assert.NotNull(state.User);
        Assert.Equal(
            "AddToRoleRejected, UserCleanupRejected",
            GetInnermostExceptionMessage(exception));
        Assert.DoesNotContain("Descrição controlada de role.", exceptionMessages, StringComparison.Ordinal);
        Assert.DoesNotContain("Descrição controlada de limpeza.", exceptionMessages, StringComparison.Ordinal);
        Assert.DoesNotContain(CreateTestPassword(), exceptionMessages, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Startup_DeveAguardarVencedorLimitadamente_QuandoCriacaoConcorrenteEstiverEntreUsuarioERole()
    {
        var state = new CoordinatedRaceState();
        using var winnerFactory = new BootstrapAdminApiFactory(
            CreateValidConfiguration(),
            SeedRoles,
            configureServices: services => ReplaceUserManager(
                services,
                new CoordinatedRaceUserManager(state, isWinner: true)));
        using var loserFactory = new BootstrapAdminApiFactory(
            CreateValidConfiguration(),
            SeedRoles,
            configureServices: services => ReplaceUserManager(
                services,
                new CoordinatedRaceUserManager(state, isWinner: false)));

        var winnerTask = Task.Run(() => winnerFactory.CreateClient());
        await state.WinnerFindStarted.Task.WaitAsync(TimeSpan.FromSeconds(10));
        var loserTask = Task.Run(() => loserFactory.CreateClient());

        var clients = await Task.WhenAll(winnerTask, loserTask)
            .WaitAsync(TimeSpan.FromSeconds(10));

        foreach (var client in clients)
        {
            client.Dispose();
        }

        Assert.True(state.IsAdmin);
    }

    [Fact]
    public async Task Startup_DeveDesistirAposDezConsultasSemPromover_QuandoConcorrenteNuncaReceberAdmin()
    {
        var state = new NeverReadyConcurrentState();
        using var factory = new BootstrapAdminApiFactory(
            CreateValidConfiguration(),
            SeedRoles,
            configureServices: services => ReplaceUserManager(
                services,
                new NeverReadyConcurrentUserManager(state)));

        var exception = await Assert.ThrowsAnyAsync<Exception>(async () =>
            await Task.Run(() => factory.CreateClient())
                .WaitAsync(TimeSpan.FromSeconds(10)));
        var failureMessage = GetInnermostExceptionMessage(exception);

        Assert.Equal(10, state.ConcurrentLookupCount);
        Assert.False(state.AddToRoleWasCalled);
        Assert.Equal("DuplicateEmail, DuplicateUserName", failureMessage);
        Assert.DoesNotContain("Descrição duplicada controlada.", failureMessage, StringComparison.Ordinal);
        Assert.DoesNotContain(CreateTestPassword(), failureMessage, StringComparison.Ordinal);
    }

    private static Dictionary<string, string?> CreateValidConfiguration()
    {
        return new Dictionary<string, string?>
        {
            ["BootstrapAdmin:Enabled"] = "true",
            ["BootstrapAdmin:NomeCompleto"] = AdminName,
            ["BootstrapAdmin:Email"] = AdminEmail,
            ["BootstrapAdmin:Password"] = CreateTestPassword()
        };
    }

    private static string CreateTestPassword()
    {
        return string.Concat("Test-Only-", Guid.Empty.ToString("N"), "!1a");
    }

    private static void SeedExistingUser(AppDbContext dbContext, string userId)
    {
        dbContext.Users.Add(new ApplicationUser
        {
            Id = userId,
            NomeCompleto = "Nome preexistente",
            Email = AdminEmail,
            NormalizedEmail = AdminEmail.ToUpperInvariant(),
            UserName = AdminEmail,
            NormalizedUserName = AdminEmail.ToUpperInvariant(),
            SecurityStamp = Guid.NewGuid().ToString(),
            ConcurrencyStamp = Guid.NewGuid().ToString()
        });
    }

    private static void SeedExistingAdmin(AppDbContext dbContext, string userId)
    {
        const string adminRoleId = "bootstrap-admin-role";
        SeedExistingUser(dbContext, userId);
        dbContext.Roles.Add(new IdentityRole
        {
            Id = adminRoleId,
            Name = "Admin",
            NormalizedName = "ADMIN",
            ConcurrencyStamp = Guid.NewGuid().ToString()
        });
        dbContext.UserRoles.Add(new IdentityUserRole<string>
        {
            UserId = userId,
            RoleId = adminRoleId
        });
    }

    private static void SeedRoles(AppDbContext dbContext)
    {
        foreach (var roleName in new[] { "Admin", "Barbeiro", "Cliente" })
        {
            dbContext.Roles.Add(new IdentityRole
            {
                Id = $"bootstrap-role-{roleName}",
                Name = roleName,
                NormalizedName = roleName.ToUpperInvariant(),
                ConcurrencyStamp = Guid.NewGuid().ToString()
            });
        }
    }

    private static void ReplaceUserManager(
        IServiceCollection services,
        UserManager<ApplicationUser> userManager)
    {
        services.RemoveAll<UserManager<ApplicationUser>>();
        services.AddScoped(_ => userManager);
    }

    private static string GetProgramPath()
    {
        var repositoryRoot = Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));

        return Path.Combine(repositoryRoot, "BarberApp.API", "Program.cs");
    }

    private static string GetExceptionMessages(Exception exception)
    {
        var messages = new List<string>();

        for (var current = exception; current is not null; current = current.InnerException)
        {
            messages.Add(current.Message);
        }

        return string.Join(Environment.NewLine, messages);
    }

    private static string GetInnermostExceptionMessage(Exception exception)
    {
        var current = exception;

        while (current.InnerException is not null)
        {
            current = current.InnerException;
        }

        return current.Message;
    }

    private sealed class RejectingRoleStore : IRoleStore<IdentityRole>
    {
        public Task<IdentityResult> CreateAsync(
            IdentityRole role,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(IdentityResult.Failed(
                new IdentityError
                {
                    Code = "RoleCreationRejected",
                    Description = "Falha controlada pelo teste."
                }));
        }

        public Task<IdentityResult> UpdateAsync(
            IdentityRole role,
            CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<IdentityResult> DeleteAsync(
            IdentityRole role,
            CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<string> GetRoleIdAsync(
            IdentityRole role,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(role.Id);
        }

        public Task<string?> GetRoleNameAsync(
            IdentityRole role,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(role.Name);
        }

        public Task SetRoleNameAsync(
            IdentityRole role,
            string? roleName,
            CancellationToken cancellationToken)
        {
            role.Name = roleName;
            return Task.CompletedTask;
        }

        public Task<string?> GetNormalizedRoleNameAsync(
            IdentityRole role,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(role.NormalizedName);
        }

        public Task SetNormalizedRoleNameAsync(
            IdentityRole role,
            string? normalizedName,
            CancellationToken cancellationToken)
        {
            role.NormalizedName = normalizedName;
            return Task.CompletedTask;
        }

        public Task<IdentityRole?> FindByIdAsync(
            string roleId,
            CancellationToken cancellationToken)
        {
            return Task.FromResult<IdentityRole?>(null);
        }

        public Task<IdentityRole?> FindByNameAsync(
            string normalizedRoleName,
            CancellationToken cancellationToken)
        {
            return Task.FromResult<IdentityRole?>(null);
        }

        public void Dispose()
        {
        }
    }

    private sealed class RejectingAddToRoleState(bool cleanupSucceeds)
    {
        public bool CleanupSucceeds { get; } = cleanupSucceeds;
        public bool DeleteWasCalled { get; set; }
        public ApplicationUser? User { get; set; }
    }

    private sealed class RejectingAddToRoleUserManager(RejectingAddToRoleState state)
        : TestUserManager
    {
        public override Task<ApplicationUser?> FindByEmailAsync(string email)
        {
            return Task.FromResult(state.User);
        }

        public override Task<IdentityResult> CreateAsync(ApplicationUser user, string password)
        {
            state.User = user;
            return Task.FromResult(IdentityResult.Success);
        }

        public override Task<IdentityResult> AddToRoleAsync(ApplicationUser user, string role)
        {
            return Task.FromResult(IdentityResult.Failed(new IdentityError
            {
                Code = "AddToRoleRejected",
                Description = "Descrição controlada de role."
            }));
        }

        public override Task<IdentityResult> DeleteAsync(ApplicationUser user)
        {
            state.DeleteWasCalled = true;

            if (state.CleanupSucceeds)
            {
                state.User = null;
                return Task.FromResult(IdentityResult.Success);
            }

            return Task.FromResult(IdentityResult.Failed(new IdentityError
            {
                Code = "UserCleanupRejected",
                Description = "Descrição controlada de limpeza."
            }));
        }
    }

    private sealed class CoordinatedRaceState
    {
        public TaskCompletionSource WinnerFindStarted { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource BothFindsStarted { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource UserCreated { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource LoserObservedUserWithoutRole { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public ApplicationUser? User { get; set; }
        public bool IsAdmin { get; set; }
        public int FindCalls;
    }

    private sealed class CoordinatedRaceUserManager(
        CoordinatedRaceState state,
        bool isWinner) : TestUserManager
    {
        public override async Task<ApplicationUser?> FindByEmailAsync(string email)
        {
            var callNumber = Interlocked.Increment(ref state.FindCalls);
            if (callNumber == 1)
            {
                state.WinnerFindStarted.TrySetResult();
            }

            if (callNumber <= 2)
            {
                if (callNumber == 2)
                {
                    state.BothFindsStarted.TrySetResult();
                }

                await state.BothFindsStarted.Task.WaitAsync(TimeSpan.FromSeconds(10));
                return null;
            }

            await state.UserCreated.Task.WaitAsync(TimeSpan.FromSeconds(10));
            return state.User;
        }

        public override async Task<IdentityResult> CreateAsync(
            ApplicationUser user,
            string password)
        {
            await state.BothFindsStarted.Task.WaitAsync(TimeSpan.FromSeconds(10));

            if (isWinner)
            {
                state.User = user;
                state.UserCreated.TrySetResult();
                return IdentityResult.Success;
            }

            await state.UserCreated.Task.WaitAsync(TimeSpan.FromSeconds(10));
            return IdentityResult.Failed(
                new IdentityError { Code = "DuplicateEmail" },
                new IdentityError { Code = "DuplicateUserName" });
        }

        public override async Task<IdentityResult> AddToRoleAsync(
            ApplicationUser user,
            string role)
        {
            await state.LoserObservedUserWithoutRole.Task.WaitAsync(TimeSpan.FromSeconds(10));
            state.IsAdmin = true;
            return IdentityResult.Success;
        }

        public override Task<bool> IsInRoleAsync(ApplicationUser user, string role)
        {
            if (!state.IsAdmin && !isWinner)
            {
                state.LoserObservedUserWithoutRole.TrySetResult();
            }

            return Task.FromResult(state.IsAdmin);
        }
    }

    private sealed class NeverReadyConcurrentState
    {
        public ApplicationUser User { get; } = new()
        {
            Id = "concurrent-bootstrap-user",
            NomeCompleto = AdminName,
            Email = AdminEmail,
            UserName = AdminEmail
        };

        public int FindCalls;
        public int ConcurrentLookupCount;
        public bool AddToRoleWasCalled { get; set; }
    }

    private sealed class NeverReadyConcurrentUserManager(NeverReadyConcurrentState state)
        : TestUserManager
    {
        public override Task<ApplicationUser?> FindByEmailAsync(string email)
        {
            var callNumber = Interlocked.Increment(ref state.FindCalls);
            if (callNumber == 1)
            {
                return Task.FromResult<ApplicationUser?>(null);
            }

            Interlocked.Increment(ref state.ConcurrentLookupCount);
            return Task.FromResult<ApplicationUser?>(state.User);
        }

        public override Task<IdentityResult> CreateAsync(
            ApplicationUser user,
            string password)
        {
            return Task.FromResult(IdentityResult.Failed(
                new IdentityError
                {
                    Code = "DuplicateEmail",
                    Description = "Descrição duplicada controlada."
                },
                new IdentityError
                {
                    Code = "DuplicateUserName",
                    Description = "Descrição duplicada controlada."
                }));
        }

        public override Task<bool> IsInRoleAsync(ApplicationUser user, string role)
        {
            return Task.FromResult(false);
        }

        public override Task<IdentityResult> AddToRoleAsync(ApplicationUser user, string role)
        {
            state.AddToRoleWasCalled = true;
            return Task.FromResult(IdentityResult.Success);
        }
    }

    private abstract class TestUserManager()
        : UserManager<ApplicationUser>(
            new StubUserStore(),
            Microsoft.Extensions.Options.Options.Create(new IdentityOptions()),
            new PasswordHasher<ApplicationUser>(),
            Array.Empty<IUserValidator<ApplicationUser>>(),
            Array.Empty<IPasswordValidator<ApplicationUser>>(),
            new UpperInvariantLookupNormalizer(),
            new IdentityErrorDescriber(),
            new ServiceCollection().BuildServiceProvider(),
            NullLogger<UserManager<ApplicationUser>>.Instance)
    {
    }

    private sealed class StubUserStore : IUserStore<ApplicationUser>
    {
        public Task<IdentityResult> CreateAsync(
            ApplicationUser user,
            CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<IdentityResult> UpdateAsync(
            ApplicationUser user,
            CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<IdentityResult> DeleteAsync(
            ApplicationUser user,
            CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<string> GetUserIdAsync(
            ApplicationUser user,
            CancellationToken cancellationToken) => Task.FromResult(user.Id);

        public Task<string?> GetUserNameAsync(
            ApplicationUser user,
            CancellationToken cancellationToken) => Task.FromResult(user.UserName);

        public Task SetUserNameAsync(
            ApplicationUser user,
            string? userName,
            CancellationToken cancellationToken)
        {
            user.UserName = userName;
            return Task.CompletedTask;
        }

        public Task<string?> GetNormalizedUserNameAsync(
            ApplicationUser user,
            CancellationToken cancellationToken) => Task.FromResult(user.NormalizedUserName);

        public Task SetNormalizedUserNameAsync(
            ApplicationUser user,
            string? normalizedName,
            CancellationToken cancellationToken)
        {
            user.NormalizedUserName = normalizedName;
            return Task.CompletedTask;
        }

        public Task<ApplicationUser?> FindByIdAsync(
            string userId,
            CancellationToken cancellationToken) => Task.FromResult<ApplicationUser?>(null);

        public Task<ApplicationUser?> FindByNameAsync(
            string normalizedUserName,
            CancellationToken cancellationToken) => Task.FromResult<ApplicationUser?>(null);

        public void Dispose()
        {
        }
    }
}
