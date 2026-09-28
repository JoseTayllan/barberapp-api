using BarberApp.Application.Services;
using BarberApp.Domain.Interfaces;
using BarberApp.Infrastructure.Data;
using BarberApp.Infrastructure.Identity;
using BarberApp.Infrastructure.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;

using FluentValidation;
using FluentValidation.AspNetCore;
using BarberApp.Infrastructure.Payment;
using BarberApp.API.Middleware;
using BarberApp.API.OpenApi;

var builder = WebApplication.CreateBuilder(args);

if (string.IsNullOrWhiteSpace(builder.Configuration["ApiKey:Value"]))
{
    throw new InvalidOperationException(
        "A configuração 'ApiKey:Value' deve conter uma API key.");
}

var bootstrapAdminEnabled = GetBootstrapAdminEnabled(builder.Configuration);
var bootstrapAdminNomeCompleto = bootstrapAdminEnabled
    ? GetRequiredConfiguration(builder.Configuration, "BootstrapAdmin:NomeCompleto")
    : null;
var bootstrapAdminEmail = bootstrapAdminEnabled
    ? GetRequiredConfiguration(builder.Configuration, "BootstrapAdmin:Email")
    : null;
var bootstrapAdminPassword = bootstrapAdminEnabled
    ? GetRequiredConfiguration(builder.Configuration, "BootstrapAdmin:Password")
    : null;

// Banco
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 6;
    options.Password.RequireUppercase = false;
    options.Password.RequireNonAlphanumeric = false;
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        policy
            .WithOrigins("http://localhost:3000")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

// JWT
var secretKey = builder.Configuration["JwtSettings:SecretKey"]!;
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["JwtSettings:Issuer"],
        ValidAudience = builder.Configuration["JwtSettings:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey))
    };
});

// Repositórios
builder.Services.AddScoped<IBarbeiroRepository, BarbeiroRepository>();
builder.Services.AddScoped<IServicoRepository, ServicoRepository>();
builder.Services.AddScoped<IAgendamentoRepository, AgendamentoRepository>();
builder.Services.AddScoped<IClienteRepository, ClienteRepository>();
builder.Services.AddScoped<IPagamentoRepository, PagamentoRepository>();
builder.Services.AddScoped<IClienteRepository, ClienteRepository>();
builder.Services.AddScoped<IDisponibilidadeRepository, DisponibilidadeRepository>();
builder.Services.AddScoped<IExcecaoAgendaRepository, ExcecaoAgendaRepository>();

// FluentValidation
builder.Services.AddValidatorsFromAssemblyContaining<BarberApp.Application.Validators.CriarBarbeiroValidator>();

// Services
builder.Services.AddScoped<BarbeiroService>();
builder.Services.AddScoped<ServicoService>();
builder.Services.AddScoped<AgendamentoService>();
builder.Services.AddScoped<ClienteService>();
builder.Services.AddScoped<TokenService>();
builder.Services.AddScoped<PagamentoService>();
builder.Services.AddScoped<IPaymentService, MockPaymentService>();
builder.Services.AddScoped<DisponibilidadeService>();
builder.Services.AddScoped<ExcecaoAgendaService>();                                                                                                                                                                                                                 

builder.Services.AddControllers()
.ConfigureApiBehaviorOptions(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var erros = context.ModelState
            .Where(e => e.Value?.Errors.Count > 0)
            .SelectMany(e => e.Value!.Errors)
            .Select(e => e.ErrorMessage)
            .ToList();

        return new Microsoft.AspNetCore.Mvc.BadRequestObjectResult(new { erros });
    };
});

builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddEndpointsApiExplorer();

// Swagger com JWT
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "BarberApp API", Version = "v1" });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Digite o token JWT. O prefixo Bearer é adicionado automaticamente."
    });

    options.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
    {
        Name = "X-API-Key",
        Type = SecuritySchemeType.ApiKey,
        In = ParameterLocation.Header,
        Description = "Informe a API key da aplicação."
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "ApiKey"
                }
            },
            Array.Empty<string>()
        }
    });

    options.OperationFilter<BearerAuthorizationOperationFilter>();
});

var app = builder.Build();

// Cria roles e, quando habilitado, o admin configurado na inicialização
using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    const string adminRoleName = "Admin";

    foreach (var role in new[] { adminRoleName, "Barbeiro", "Cliente" })
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            var createRoleResult = await roleManager.CreateAsync(new IdentityRole(role));
            if (!createRoleResult.Succeeded && !await roleManager.RoleExistsAsync(role))
            {
                throw CreateIdentityException(createRoleResult);
            }
        }
    }

    if (bootstrapAdminEnabled)
    {
        var admin = await userManager.FindByEmailAsync(bootstrapAdminEmail!);
        if (admin is not null)
        {
            if (!await userManager.IsInRoleAsync(admin, adminRoleName))
            {
                throw new InvalidOperationException("BootstrapAdminExistingUserIsNotAdmin");
            }
        }
        else
        {
            var newAdmin = new ApplicationUser
            {
                NomeCompleto = bootstrapAdminNomeCompleto!,
                Email = bootstrapAdminEmail,
                UserName = bootstrapAdminEmail
            };

            var createResult = await userManager.CreateAsync(newAdmin, bootstrapAdminPassword!);
            if (!createResult.Succeeded)
            {
                if (!IsDuplicateIdentityFailure(createResult))
                {
                    throw CreateIdentityException(createResult);
                }

                var concurrentAdminIsReady = await WaitForConcurrentAdminAsync(
                    userManager,
                    bootstrapAdminEmail!,
                    adminRoleName);
                if (!concurrentAdminIsReady)
                {
                    throw CreateIdentityException(createResult);
                }
            }
            else
            {
                var addToRoleResult = await userManager.AddToRoleAsync(newAdmin, adminRoleName);
                if (!addToRoleResult.Succeeded)
                {
                    var cleanupResult = await userManager.DeleteAsync(newAdmin);
                    throw cleanupResult.Succeeded
                        ? CreateIdentityException(addToRoleResult)
                        : CreateIdentityException(addToRoleResult, cleanupResult);
                }
            }
        }
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors("Frontend");

app.UseMiddleware<ApiKeyAuthenticationMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

static bool GetBootstrapAdminEnabled(IConfiguration configuration)
{
    const string key = "BootstrapAdmin:Enabled";
    var value = configuration[key];

    if (value is null)
    {
        return false;
    }

    if (string.IsNullOrWhiteSpace(value) || !bool.TryParse(value, out var enabled))
    {
        throw new InvalidOperationException(
            $"A configuração '{key}' deve ser um booleano válido.");
    }

    return enabled;
}

static string GetRequiredConfiguration(IConfiguration configuration, string key)
{
    var value = configuration[key];
    if (string.IsNullOrWhiteSpace(value))
    {
        throw new InvalidOperationException(
            $"A configuração '{key}' deve conter um valor.");
    }

    return value;
}

static bool IsDuplicateIdentityFailure(IdentityResult result)
{
    var errors = result.Errors.ToArray();
    return errors.Length > 0 && errors.All(error =>
        error.Code is "DuplicateEmail" or "DuplicateUserName");
}

static async Task<bool> WaitForConcurrentAdminAsync(
    UserManager<ApplicationUser> userManager,
    string email,
    string adminRoleName)
{
    const int maximumAttempts = 10;
    var retryDelay = TimeSpan.FromMilliseconds(100);

    for (var attempt = 0; attempt < maximumAttempts; attempt++)
    {
        var concurrentAdmin = await userManager.FindByEmailAsync(email);
        if (concurrentAdmin is not null &&
            await userManager.IsInRoleAsync(concurrentAdmin, adminRoleName))
        {
            return true;
        }

        if (attempt < maximumAttempts - 1)
        {
            await Task.Delay(retryDelay);
        }
    }

    return false;
}

static InvalidOperationException CreateIdentityException(params IdentityResult[] results)
{
    var errorCodes = string.Join(", ", results
        .SelectMany(result => result.Errors)
        .Select(error => error.Code));
    return new InvalidOperationException(errorCodes);
}
