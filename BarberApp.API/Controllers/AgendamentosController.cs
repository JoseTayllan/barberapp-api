using System.Security.Claims;
using BarberApp.Application.DTOs;
using BarberApp.Application.Services;
using BarberApp.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BarberApp.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AgendamentosController : ControllerBase
{
    private readonly AgendamentoService _service;
    private readonly ClienteService _clienteService;

    public AgendamentosController(AgendamentoService service, ClienteService clienteService)
    {
        _service = service;
        _clienteService = clienteService;

    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> Listar()
    {
        IEnumerable<Agendamento> agendamentos;

        if (User.IsInRole("Admin"))
        {
            agendamentos = await _service.ListarTodosAsync();
        }
        else if (User.IsInRole("Barbeiro"))
        {
            var barbeiroIdStr = User.FindFirstValue("BarbeiroId");
            if (barbeiroIdStr is null)
                return BadRequest(new { mensagem = "Barbeiro não vinculado ao usuário." });

            agendamentos = await _service.ListarPorBarbeiroAsync(Guid.Parse(barbeiroIdStr));
        }
        else
        {
            var emailCliente = User.FindFirstValue(ClaimTypes.Email)!;
            var cliente = await _clienteService.ObterPorEmailAsync(emailCliente);

            if (cliente is null)
                return NotFound(new { mensagem = "Perfil de cliente não encontrado." });

            agendamentos = await _service.ListarPorClienteAsync(cliente.Id);
        }

        var response = agendamentos.Select(a => new AgendamentoResponse(
            a.Id,
            a.Cliente!.Nome,
            a.Barbeiro!.Nome,
            a.Servico!.Nome,
            a.Servico!.Preco,
            a.DataHora,
            a.Status.ToString()));

        return Ok(response);
    }

    [HttpGet("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> ObterPorId(Guid id)
    {
        var a = await _service.ObterPorIdAsync(id);

        if (a is null)
            return NotFound(new { mensagem = "Agendamento não encontrado." });

        return Ok(new AgendamentoResponse(
            a.Id,
            a.Cliente!.Nome,
            a.Barbeiro!.Nome,
            a.Servico!.Nome,
            a.Servico!.Preco,
            a.DataHora,
            a.Status.ToString()));
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Criar([FromBody] CriarAgendamentoRequest request)
    {
        try
        {
            var email = User.FindFirstValue(ClaimTypes.Email)!;

            var agendamento = await _service.CriarAsync(
                email,
                request.BarbeiroId,
                request.ServicoId,
                request.DataHora,
                request.Observacao);

            return CreatedAtAction(nameof(ObterPorId), new { id = agendamento.Id },
                new { agendamento.Id, agendamento.DataHora, Status = agendamento.Status.ToString() });
        }
        catch (Exception ex)
        {
            return BadRequest(new { mensagem = ex.Message });
        }
    }

    [HttpPatch("{id:guid}/confirmar")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Confirmar(Guid id)
    {
        try
        {
            await _service.ConfirmarAsync(id);
            return NoContent();
        }
        catch (Exception ex)
        {
            return BadRequest(new { mensagem = ex.Message });
        }
    }

    [HttpPatch("{id:guid}/cancelar")]
    [Authorize]
    public async Task<IActionResult> Cancelar(Guid id)
    {
        try
        {
            await _service.CancelarAsync(id);
            return NoContent();
        }
        catch (Exception ex)
        {
            return BadRequest(new { mensagem = ex.Message });
        }
    }
    [HttpGet("horarios-disponiveis")]
    [AllowAnonymous]
    public async Task<IActionResult> HorariosDisponiveis(
    [FromQuery] Guid barbeiroId,
    [FromQuery] Guid servicoId,
    [FromQuery] string data)
    {
        try
        {
            // Aceita formato brasileiro dd/MM/yyyy ou internacional yyyy-MM-dd
            var formatos = new[] { "dd/MM/yyyy", "yyyy-MM-dd" };

            if (!DateTime.TryParseExact(data, formatos,
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None,
                out var dataParsed))
            {
                return BadRequest(new { mensagem = "Formato de data inválido. Use: 10/04/2026 ou 2026-04-10" });
            }

            var horarios = await _service.ObterHorariosDisponiveisAsync(
                barbeiroId, servicoId, dataParsed.Date);

            return Ok(horarios);
        }
        catch (Exception ex)
        {
            return BadRequest(new { mensagem = ex.Message });
        }
    }
}