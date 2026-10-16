using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Restaurante.Api.Dtos;
using Restaurante.Api.Resultado;
using Restaurante.Api.Servicos;

namespace Restaurante.Api.Controllers;

[ApiController]
[Route("api/mesas")]
public class MesasController : ControllerBase
{
    private readonly RestauranteServico _servico;

    public MesasController(RestauranteServico servico)
    {
        _servico = servico;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Listar([FromQuery] string? busca, [FromQuery] int pagina = 1, [FromQuery] int tamanho = 10)
    {
        var resultado = await _servico.ListarMesasAsync(busca, pagina, tamanho);
        return Ok(resultado);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(MesaRespostaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ObterPorId(int id)
    {
        var resultado = await _servico.ObterMesaPorIdAsync(id);
        if (!resultado.Sucesso) return NotFound(new { mensagem = resultado.MensagemErro });
        return Ok(resultado.Valor);
    }

    [HttpPost]
    [Authorize(Roles = "admin")] // Apenas admin na escrita da entidade principal
    [ProducesResponseType(typeof(MesaRespostaDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Criar([FromBody] CriarMesaDto dto)
    {
        var resultado = await _servico.CriarMesaAsync(dto);
        if (!resultado.Sucesso) return BadRequest(new { mensagem = resultado.MensagemErro });
        return CreatedAtAction(nameof(ObterPorId), new { id = resultado.Valor!.Id }, resultado.Valor);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "admin")]
    [ProducesResponseType(typeof(MesaRespostaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Atualizar(int id, [FromBody] AtualizarMesaDto dto)
    {
        var resultado = await _servico.AtualizarMesaAsync(id, dto);
        if (!resultado.Sucesso) return NotFound(new { mensagem = resultado.MensagemErro });
        return Ok(resultado.Valor);
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Deletar(int id)
    {
        var resultado = await _servico.DeletarMesaAsync(id);
        if (!resultado.Sucesso) return NotFound(new { mensagem = resultado.MensagemErro });
        return NoContent();
    }
}