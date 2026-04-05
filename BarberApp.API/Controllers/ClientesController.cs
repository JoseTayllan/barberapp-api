using System.Security.Claims;
using BarberApp.Application.DTOs;
using BarberApp.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BarberApp.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ClientesController : ControllerBase
{
    private readonly ClienteService _service;

    public ClientesController(ClienteService service)
    {
        _service = service;
    }

    [HttpGet("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> ObterPorId(Guid id)
    {
        var cliente = await _service.ObterPorIdAsync(id);

        if (cliente is null)
            return NotFound(new { mensagem = "Cliente não encontrado." });

        return Ok(new ClienteResponse(
            cliente.Id, cliente.Nome, cliente.Email, cliente.Telefone));
    }

    [HttpPost]
    [AllowAnonymous] // público
    public async Task<IActionResult> Criar([FromBody] CriarClienteRequest request)
    {
        try
        {
            var cliente = await _service.CriarAsync(
                request.Nome, request.Email, request.Telefone);

            return CreatedAtAction(nameof(ObterPorId), new { id = cliente.Id },
                new ClienteResponse(cliente.Id, cliente.Nome, cliente.Email, cliente.Telefone));
        }
        catch (Exception ex)
        {
            return BadRequest(new { mensagem = ex.Message });
        }
    }

    [HttpGet("meu-perfil")]
    [Authorize]
    public async Task<IActionResult> MeuPerfil()
    {
        var email = User.FindFirstValue(ClaimTypes.Email)!;
        var clientes = await _service.ObterPorEmailAsync(email);

        if (clientes is null)
            return NotFound(new { mensagem = "Perfil de cliente não encontrado. Cadastre-se em POST /api/clientes." });

        return Ok(new ClienteResponse(
            clientes.Id, clientes.Nome, clientes.Email, clientes.Telefone));
    }
}