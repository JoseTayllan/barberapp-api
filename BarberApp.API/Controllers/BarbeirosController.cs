using BarberApp.Application.DTOs;
using BarberApp.Application.Services;
using Microsoft.AspNetCore.Mvc;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using BarberApp.Infrastructure.Identity;

namespace BarberApp.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BarbeirosController : ControllerBase
{
    private readonly BarbeiroService _service;
    private readonly UserManager<ApplicationUser> _userManager;

    public BarbeirosController(BarbeiroService service, UserManager<ApplicationUser> userManager)
    {
        _service = service;
        _userManager = userManager;
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
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult>Criar([FromBody] CriarBarbeiroComLoginRequest request)
    {
        // 1. Criar a entidade Barbeiro
        var barbeiro = await _service.CriarAsync(request.NomeCompleto, request.Telefone, request.Foto);

        //2. Criar o ApplicationUser vinculado

        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            NomeCompleto = request.NomeCompleto,
            BarbeiroId = barbeiro.Id
        };

        var result = await _userManager.CreateAsync(user, request.Senha);

        if (!result.Succeeded)
        {
            // Se falhou, deletar o barbeiro criado para manter a consistência. Se falhou criar o login, desfaz a entidade
            await _service.DesativarAsync(barbeiro.Id);
            return BadRequest(new { mensagem = "Erro ao criar usuário: " + string.Join(", ", result.Errors.Select(e => e.Description)) });
        }

        await _userManager.AddToRoleAsync(user, "Barbeiro");

        return CreatedAtAction(nameof(ObterPorId), new { id = barbeiro.Id },
         new BarberiroComLoginRespnse(
            barbeiro.Id,
            user.Id,
            barbeiro.Nome,
            user.Email,
            barbeiro.Telefone));
    }






    // [Authorize(Roles = "Admin")] // só admin
    // public async Task<IActionResult> Criar([FromBody] BarbeiroCreateRequest request)
    // {
    //     var barbeiro = await _service.CriarAsync(
    //         request.Nome, request.Telefone, request.Foto);

    //     var response = new BarbeiroResponse(
    //         barbeiro.Id, barbeiro.Nome, barbeiro.Telefone, barbeiro.Foto);

    //     return CreatedAtAction(nameof(ObterPorId), new { id = barbeiro.Id }, response);
    // }
}