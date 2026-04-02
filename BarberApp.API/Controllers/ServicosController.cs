using BarberApp.Application.DTOs;
using BarberApp.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BarberApp.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ServicosController : ControllerBase
{
    private readonly ServicoService _service;

    public ServicosController(ServicoService service)
    {
        _service = service;
    }

    [HttpGet]
    [AllowAnonymous] // público
    public async Task<IActionResult> Listar()
    {
        var servicos = await _service.ListarAtivosAsync();

        var response = servicos.Select(s => new ServicoResponse(
            s.Id, s.Nome, s.Descricao, s.Preco, s.DuracaoMinuto));

        return Ok(response);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Criar([FromBody] CriarServicoRequest request)
    {
        var servico = await _service.CriarAsync(
            request.Nome, request.Preco, request.DuracaoMinutos, request.Descricao);

        var response = new ServicoResponse(
            servico.Id, servico.Nome, servico.Descricao, servico.Preco, servico.DuracaoMinuto);

        return CreatedAtAction(nameof(Listar), response);
    }
}