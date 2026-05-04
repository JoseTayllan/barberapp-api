using BarberApp.Application.DTOs;
using BarberApp.Application.Services;
using BarberApp.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
namespace BarberApp.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly TokenService _tokenService;
    private readonly ClienteService _clienteService;

    public AuthController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        TokenService tokenService,
        ClienteService clienteService)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _tokenService = tokenService;
        _clienteService = clienteService;
    }

    [HttpPost("registro")]
    public async Task<IActionResult> Registro([FromBody] RegistroRequest request)
    {
        var user = new ApplicationUser
        {
            NomeCompleto = request.NomeCompleto,
            Email = request.Email,
            UserName = request.Email
        };

        var resultado = await _userManager.CreateAsync(user, request.Password);

        if (!resultado.Succeeded)
            return BadRequest(new { erros = resultado.Errors.Select(e => e.Description) });

        await _userManager.AddToRoleAsync(user, "Cliente");

        // Cria o perfil de cliente automaticamente
        await _clienteService.CriarAsync(request.NomeCompleto, request.Email, request.Telefone );

        var roles = await _userManager.GetRolesAsync(user);
        var token = _tokenService.GerarToken(user.Id, user.Email!, user.NomeCompleto, roles, user.BarbeiroId);

        return Ok(new AuthResponse(
            token,
            user.NomeCompleto,
            user.Email!,
            roles,
            DateTime.UtcNow.AddHours(8)
        ));
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);

        if (user is null)
            return Unauthorized(new { mensagem = "E-mail ou senha inválidos." });

        var resultado = await _signInManager.CheckPasswordSignInAsync(user, request.Password, false);

        if (!resultado.Succeeded)
            return Unauthorized(new { mensagem = "E-mail ou senha inválidos." });

        var roles = await _userManager.GetRolesAsync(user);
        var token = _tokenService.GerarToken(user.Id, user.Email!, user.NomeCompleto, roles, user.BarbeiroId);

        return Ok(new AuthResponse(token, user.NomeCompleto, user.Email!, roles, DateTime.UtcNow.AddHours(8)));
    }
}