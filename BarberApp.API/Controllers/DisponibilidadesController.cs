using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using BarberApp.Application.DTOs;
using BarberApp.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BarberApp.API.Controllers
{
    [ApiController]
    [Route("api/barbeiros/{barbeiroId:guid}/disponibilidades")]
    public class DisponibilidadesController : ControllerBase
    {
        private readonly DisponibilidadeService _service;

        public DisponibilidadesController(DisponibilidadeService service)
        {
            _service = service;
        }
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Listar(Guid barbeiroId)
        {
            var disponibilidade = await _service.ListarPorBarbeiroAsync(barbeiroId);

            var response = disponibilidade.Select(d => new DisponibildadeResponse(
                d.Id,
                d.DiaSemana.ToString(),
                d.HoraInicio.ToString(@"hh\:mm"),
                d.HoraFim.ToString(@"hh\:mm"),
                d.Ativo));
            return Ok(response);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Criar(Guid barbeiroId,[FromBody] CriarDisponibilidadeRequest request)
        {
            try
            {
                var disponibilidade = await _service.CriarAsync(
                    barbeiroId,
                    request.DiaSemana,
                    request.HorarioInicio,
                    request.HorarioFim);
                return CreatedAtAction(nameof(Listar), new { barbeiroId },
                new DisponibildadeResponse(
                    disponibilidade.Id,
                    disponibilidade.DiaSemana.ToString(),
                    disponibilidade.HoraInicio.ToString(@"hh\:mm"),
                    disponibilidade.HoraFim.ToString(@"hh\:mm"),
                    disponibilidade.Ativo));
            }catch(Exception ex)
            {
                return BadRequest(new { mensagem = ex.Message });
            }
        }
    }
}