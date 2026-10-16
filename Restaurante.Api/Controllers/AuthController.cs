using Microsoft.AspNetCore.Mvc;
using Restaurante.Api.Dtos;
using Restaurante.Api.Resultado;
using Restaurante.Api.Servicos;

namespace Restaurante.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly RestauranteServico _servico;

    public AuthController(RestauranteServico servico)
    {
        _servico = servico;
    }

    [HttpPost("registrar")]
    [ProducesResponseType(typeof(TokenDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Registrar([FromBody] RegistrarDto dto)
    {
        var resultado = await _servico.RegistrarAsync(dto);
        if (!resultado.Sucesso)
        {
            return resultado.Tipo == TipoErro.Conflito 
                ? Conflict(new { mensagem = resultado.MensagemErro }) 
                : BadRequest(new { mensagem = resultado.MensagemErro });
        }
        return StatusCode(StatusCodes.Status201Created, resultado.Valor);
    }

    [HttpPost("login")]
    [ProducesResponseType(typeof(TokenDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        var resultado = await _servico.LoginAsync(dto);
        if (!resultado.Sucesso)
        {
            return BadRequest(new { mensagem = resultado.MensagemErro });
        }
        return Ok(resultado.Valor);
    }
}