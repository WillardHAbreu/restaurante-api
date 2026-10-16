using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Restaurante.Api.Dtos;
using Restaurante.Api.Resultado;
using Restaurante.Api.Servicos;

namespace Restaurante.Api.Controllers;

[ApiController]
[Route("api/reservas")]
[Authorize]
public class ReservasController : ControllerBase
{
    private readonly RestauranteServico _servico;

    public ReservasController(RestauranteServico servico)
    {
        _servico = servico;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<ReservaRespostaDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Listar()
    {
        var usuarioIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var papel = User.FindFirstValue(ClaimTypes.Role) ?? "cliente";

        if (!int.TryParse(usuarioIdStr, out var usuarioId))
        {
            return Unauthorized();
        }

        var reservas = await _servico.ListarReservaAsync(usuarioId, papel);
        return Ok(reservas);
    }

    [HttpPost]
    [ProducesResponseType(typeof(ReservaRespostaDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)] // Regra de negócio (mesa ocupada ou capacidade excedida)
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Criar([FromBody] CriarReservaDto dto)
    {
        var usuarioIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(usuarioIdStr, out var usuarioId))
        {
            return Unauthorized();
        }

        var resultado = await _servico.CriarReservaAsync(dto, usuarioId);
        if (!resultado.Sucesso)
        {
            return resultado.Tipo == TipoErro.Conflito
                ? Conflict(new { mensagem = resultado.MensagemErro })
                : BadRequest(new { mensagem = resultado.MensagemErro });
        }

        return StatusCode(StatusCodes.Status201Created, resultado.Valor);
    }
}