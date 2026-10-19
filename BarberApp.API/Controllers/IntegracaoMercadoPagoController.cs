using System.Security.Claims;
using BarberApp.Application.DTOs;
using BarberApp.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BarberApp.API.Controllers;

[ApiController]
[Route("api/integracoes/mercado-pago")]
[Authorize(Roles = "Admin")]
public sealed class IntegracaoMercadoPagoController(
    IConexaoMercadoPagoService service, IRenovacaoMercadoPagoService renovacaoService) : ControllerBase
{
    [HttpPost("renovacao")]
    public async Task<ActionResult<StatusConexaoMercadoPagoResponse>> RenovarAsync(CancellationToken cancellationToken)
    {
        Response.Headers.CacheControl = "no-store";
        if (string.IsNullOrWhiteSpace(User.FindFirstValue(ClaimTypes.NameIdentifier)))
            return Unauthorized();

        try
        {
            return Ok(await renovacaoService.RenovarAsync(cancellationToken));
        }
        catch (MercadoPagoNaoConfiguradoException)
        {
            return Problem(statusCode: StatusCodes.Status503ServiceUnavailable, title: "MercadoPagoNaoConfigurado");
        }
        catch (MercadoPagoOperacaoException exception)
        {
            var status = exception.Codigo switch
            {
                "MercadoPagoNaoConectado" or "MercadoPagoReautorizacaoNecessaria" => StatusCodes.Status409Conflict,
                "MercadoPagoProvedorFalhou" or "MercadoPagoRespostaInvalida" => StatusCodes.Status502BadGateway,
                _ => StatusCodes.Status503ServiceUnavailable
            };
            return Problem(statusCode: status, title: exception.Codigo);
        }
    }

    [HttpGet("status")]
    public async Task<ActionResult<StatusConexaoMercadoPagoResponse>> Status(CancellationToken cancellationToken)
    {
        Response.Headers.CacheControl = "no-store";
        if (string.IsNullOrWhiteSpace(User.FindFirstValue(ClaimTypes.NameIdentifier)))
            return Unauthorized();

        try
        {
            return Ok(await service.ConsultarStatusAsync(cancellationToken));
        }
        catch (MercadoPagoOperacaoException exception) when (exception.Codigo == "MercadoPagoConsultaFalhou")
        {
            return Problem(statusCode: StatusCodes.Status503ServiceUnavailable, title: exception.Codigo);
        }
    }

    [AllowAnonymous]
    [HttpGet("callback")]
    public async Task<ActionResult<MercadoPagoCallbackResponse>> Callback(
        [FromQuery] string? state, [FromQuery] string? code, [FromQuery] string? error,
        CancellationToken cancellationToken)
    {
        Response.Headers.CacheControl = "no-store";
        Response.Headers["Referrer-Policy"] = "no-referrer";
        if (new[] { "state", "code", "error" }.Any(key => Request.Query[key].Count > 1))
            return CallbackInvalido();

        try
        {
            var resultado = await service.ProcessarRetornoAsync(state, code, error, cancellationToken);
            if (resultado == ResultadoRetornoMercadoPago.AutorizacaoRecusada)
                return Ok(new MercadoPagoCallbackResponse("AutorizacaoRecusada"));
            if (resultado == ResultadoRetornoMercadoPago.Conectado)
                return Ok(new MercadoPagoCallbackResponse("Conectado"));
            return Problem(statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "MercadoPagoTrocaTokenPendente");
        }
        catch (MercadoPagoCallbackInvalidoException)
        {
            return CallbackInvalido();
        }
        catch (MercadoPagoNaoConfiguradoException)
        {
            return Problem(statusCode: StatusCodes.Status503ServiceUnavailable, title: "MercadoPagoNaoConfigurado");
        }
        catch (MercadoPagoOperacaoException exception)
        {
            var status = exception.Codigo is "MercadoPagoProvedorFalhou" or "MercadoPagoRespostaInvalida"
                ? StatusCodes.Status502BadGateway : StatusCodes.Status503ServiceUnavailable;
            return Problem(statusCode: status, title: exception.Codigo);
        }
    }

    private ObjectResult CallbackInvalido() =>
        Problem(statusCode: StatusCodes.Status400BadRequest, title: "MercadoPagoCallbackInvalido");

    [HttpPost("autorizacao")]
    public ActionResult<IniciarConexaoMercadoPagoResponse> Iniciar()
    {
        Response.Headers.CacheControl = "no-store";
        var administradorId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(administradorId))
            return Unauthorized();

        try
        {
            return Ok(service.Iniciar(administradorId));
        }
        catch (MercadoPagoNaoConfiguradoException)
        {
            return Problem(statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "MercadoPagoNaoConfigurado");
        }
    }
}
