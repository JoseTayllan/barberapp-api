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

        public PagamentosController(PagamentoService Service)
        {
            _Service = Service;
        }

        [HttpPost("{agendamentoId:guid}")]
        public async Task<IActionResult>Processar (Guid agendamentoId)
        {
            try
            {
                var email = User.FindFirstValue(ClaimTypes.Email)!;
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