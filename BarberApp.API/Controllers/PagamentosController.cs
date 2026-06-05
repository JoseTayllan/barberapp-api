using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using BarberApp.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BarberApp.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class PagamentosController : ControllerBase
    {
        private readonly PagamentoService _Service;
        private readonly AgendamentoService _agendamentoService;
        private readonly ClienteService _clienteService;

        public PagamentosController(PagamentoService Service, AgendamentoService agendamentoService, ClienteService clienteService)
        {
            _Service = Service;
            _agendamentoService = agendamentoService;
            _clienteService = clienteService;
        }

        [HttpPost("{agendamentoId:guid}")]
        public async Task<IActionResult>Processar (Guid agendamentoId)
        {
            try
            {
                var email = User.FindFirstValue(ClaimTypes.Email)!;
                // Valida se o agendamento pertence ao cliente logado
                if (User.IsInRole("Cliente"))
                {
                    var agendamento = await _agendamentoService.ObterPorIdAsync(agendamentoId);

                    if (agendamento is null)
                        return NotFound(new { mensagem = "Agendamento não encontrado." });

                    var cliente = await _clienteService.ObterPorEmailAsync(email);

                    if (agendamento.ClienteId != cliente!.Id)
                        return Forbid();
                }
                
                var pagamento = await _Service.ProcessarAsync(agendamentoId, email);

                return Ok(new
                {
                    pagamento.Id,
                    pagamento.Valor,
                    Status = pagamento.Status.ToString(),
                    pagamento.GatewayTransacaoId,
                    pagamento.Gateway

                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Mensagem = ex.Message });
            }
        }
    }
}