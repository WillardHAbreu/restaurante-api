using System.ComponentModel.DataAnnotations;

namespace Restaurante.Api.Dtos;

public record RegistrarDto(
    [Required] string Nome,
    [Required][EmailAddress] string Email,
    [Required][MinLength(6)] string Senha
);

public record LoginDto(
    [Required][EmailAddress] string Email,
    [Required] string Senha
);

public record TokenDto(
    string Token,
    string Email,
    string Papel
);