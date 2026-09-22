

using System.Security.Claims;
using BarberApp.Application.DTOs;
using BarberApp.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BarberApp.API.Controllers
{
        [ApiController]
        [Route("api/barbeiros/{barbeiroId:guid}/excecoes")]
        public class ExcecoesAgendaController : ControllerBase
        {
            private readonly ExcecaoAgendaService _service;

            public ExcecoesAgendaController(ExcecaoAgendaService service)
            {
                _service = service;
            }

            [HttpGet]
            [AllowAnonymous]
            public async Task<IActionResult> Listar(Guid barbeiroId)
            {
                var excecoes = await _service.ListarPorBarbeiroAsync(barbeiroId);

                var response = excecoes.Select(e => new ExcecaoAgendaResponse(
                    e.Id,
                    e.Data.ToString("yyyy-MM-dd"),
                    e.NaoAtende,
                    e.HoraInicio?.ToString(@"hh\:mm"),
                    e.HoraFim?.ToString(@"hh\:mm"),
                    e.Motivo
                ));

                return Ok(response);
            }

            [HttpPost]
            [Authorize(Roles = "Admin,Barbeiro")]
            public async Task<IActionResult> Criar(Guid barbeiroId, [FromBody] CriarExcecaoAgendaRequest request)
            {
                if (User.IsInRole("Barbeiro"))
                {
                    var userBarbeiroIdStr = User.FindFirstValue("BarbeiroId");
                    if (!Guid.TryParse(userBarbeiroIdStr, out var userBarbeiroId) || userBarbeiroId != barbeiroId)
                        return Forbid();
                }
                try
                {
                    var excecao = await _service.CriarAsync(
                        barbeiroId, request.Data, request.NaoAtende,
                        request.HorarioInicio, request.HorarioFim, request.Motivo);

                    return CreatedAtAction(nameof(Listar), new { barbeiroId },
                        new ExcecaoAgendaResponse(
                            excecao.Id,
                            excecao.Data.ToString("dd/MM/yyyy"),
                            excecao.NaoAtende, excecao.HoraInicio?.ToString(@"hh\:mm"),
                            excecao.HoraFim?.ToString(@"hh\:mm"),
                            excecao.Motivo
                            ));
                }
                catch (Exception ex)
                {
                    return BadRequest(new { message = ex.Message });
                }
            }

            [HttpDelete("{data}")]
            [Authorize(Roles = "Admin,Barbeiro")]
            public async Task<IActionResult> Remover(Guid barbeiroId, string data)
            {
                if (User.IsInRole("Barbeiro"))
                {
                    var userBarbeiroIdStr = User.FindFirstValue("BarbeiroId");
                    if (!Guid.TryParse(userBarbeiroIdStr, out var userbarbeiroId) || userbarbeiroId != barbeiroId)
                        return Forbid();
                }
                try
                {
                    await _service.RemoverAsync(barbeiroId, data);
                    return NoContent();
                } catch (Exception ex)
                {
                    return BadRequest(new { message = ex.Message });
                    
                }
        
            }

        }
}