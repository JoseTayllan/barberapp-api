using BarberApp.Application.DTOs;
using BarberApp.Application.Services;
using BarberApp.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BarberApp.API.Controllers;

[ApiController]
[Route("api/perfil")]
[Authorize]
public class PerfilController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ClienteService _clienteService;
    private readonly BarbeiroService _barbeiroService;

    public PerfilController(
        UserManager<ApplicationUser> userManager,
        ClienteService clienteService,
        BarbeiroService barbeiroService)
    {
        _userManager = userManager;
        _clienteService = clienteService;
        _barbeiroService = barbeiroService;
    }

    [HttpGet]
    public async Task<IActionResult> ObterPerfil()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var user = await _userManager.FindByIdAsync(userId);

        if (user is null)
            return NotFound(new { mensagem = "Usuário não encontrado." });

        var roles = await _userManager.GetRolesAsync(user);

        // Busca telefone dependendo da role
        var telefone = "";
        if (User.IsInRole("Cliente"))
        {
            var cliente = await _clienteService.ObterPorEmailAsync(user.Email!);
            telefone = cliente?.Telefone ?? "";
        }
        else if (User.IsInRole("Barbeiro") && user.BarbeiroId.HasValue)
        {
            var barbeiro = await _barbeiroService.ObterPorIdAsync(user.BarbeiroId.Value);
            telefone = barbeiro?.Telefone ?? "";
        }

        return Ok(new PerfilResponse(
            user.Id,
            user.NomeCompleto,
            user.Email!,
            telefone,
            roles));
    }

    [HttpPut]
    public async Task<IActionResult> AtualizarPerfil([FromBody] AtualizarPerfilRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var user = await _userManager.FindByIdAsync(userId);

        if (user is null)
            return NotFound(new { mensagem = "Usuário não encontrado." });

        // Atualiza ApplicationUser
        user.NomeCompleto = request.NomeCompleto;
        var resultado = await _userManager.UpdateAsync(user);

        if (!resultado.Succeeded)
            return BadRequest(new { erros = resultado.Errors.Select(e => e.Description) });

        // Atualiza entidade vinculada
        if (User.IsInRole("Cliente"))
        {
            await _clienteService.AtualizarAsync(user.Email!, request.NomeCompleto, request.Telefone);
        }
        else if (User.IsInRole("Barbeiro") && user.BarbeiroId.HasValue)
        {
            await _barbeiroService.AtualizarNomeAsync(user.BarbeiroId.Value, request.NomeCompleto, request.Telefone);
        }

        var roles = await _userManager.GetRolesAsync(user);

        return Ok(new PerfilResponse(
            user.Id,
            user.NomeCompleto,
            user.Email!,
            request.Telefone,
            roles));
    }

    [HttpPatch("alterar-senha")]
    public async Task<IActionResult> AlterarSenha([FromBody] AlterarSenhaRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var user = await _userManager.FindByIdAsync(userId);

        if (user is null)
            return NotFound(new { mensagem = "Usuário não encontrado." });

        var resultado = await _userManager.ChangePasswordAsync(
            user, request.SenhaAtual, request.NovaSenha);

        if (!resultado.Succeeded)
            return BadRequest(new { erros = resultado.Errors.Select(e => e.Description) });

        return Ok(new { mensagem = "Senha alterada com sucesso." });
    }
}