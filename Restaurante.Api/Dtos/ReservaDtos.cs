using System.ComponentModel.DataAnnotations;

namespace Restaurante.Api.Dtos;

public record CriarReservaDto(
    [Required] int MesaId,
    [Required] int NumeroPessoas,
    [Required] DateTimeOffset DataHora
);

public record ReservaRespostaDto(
    int Id,
    int MesaId,
    string NumeroMesa,
    int UsuarioId,
    string NomeUsuario,
    int NumeroPessoas,
    DateTimeOffset DataHora,
    string Status
);