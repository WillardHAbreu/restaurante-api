using System.ComponentModel.DataAnnotations;

namespace Restaurante.Api.Dtos;

public record CriarMesaDto(
    [Required][MaxLength(100)] string NumeroOuNome,
    [Range(1, 20)] int CapacidadePessoas,
    [MaxLength(200)] string Localizacao
);

public record AtualizarMesaDto(
    [Required][MaxLength(100)] string NumeroOuNome,
    [Range(1, 20)] int CapacidadePessoas,
    [MaxLength(200)] string Localizacao
);

public record MesaRespostaDto(
    int Id,
    string NumeroOuNome,
    int CapacidadePessoas,
    string Localizacao
);