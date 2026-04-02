using BarberApp.Application.DTOs;
using BarberApp.Application.Services;
using Microsoft.AspNetCore.Mvc;

using Microsoft.AspNetCore.Authorization;

namespace BarberApp.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BarbeirosController : ControllerBase
{
    private readonly BarbeiroService _service;

    public BarbeirosController(BarbeiroService service)
    {
        _service = service;
    }

    [HttpGet]
    [AllowAnonymous] // público
    public async Task<IActionResult> Listar()
    {
        var barbeiros = await _service.ListarAtivosAsync();

        var response = barbeiros.Select(b => new BarbeiroResponse(
            b.Id, b.Nome, b.Telefone, b.Foto));

        return Ok(response);
    }

    [HttpGet("{id:guid}")]
    [AllowAnonymous] // público
    public async Task<IActionResult> ObterPorId(Guid id)
    {
        var barbeiro = await _service.ObterPorIdAsync(id);

        if (barbeiro is null)
            return NotFound(new { mensagem = "Barbeiro não encontrado." });

        return Ok(new BarbeiroResponse(
            barbeiro.Id, barbeiro.Nome, barbeiro.Telefone, barbeiro.Foto));
    }

    [HttpPost]
    [Authorize(Roles = "Admin")] // só admin
    public async Task<IActionResult> Criar([FromBody] BarbeiroCreateRequest request)
    {
        var barbeiro = await _service.CriarAsync(
            request.Nome, request.Telefone, request.Foto);

        var response = new BarbeiroResponse(
            barbeiro.Id, barbeiro.Nome, barbeiro.Telefone, barbeiro.Foto);

        return CreatedAtAction(nameof(ObterPorId), new { id = barbeiro.Id }, response);
    }
}