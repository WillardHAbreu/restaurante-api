using System.ComponentModel.DataAnnotations;

namespace Restaurante.Api.Modelos;

public class Usuario
{
    public int Id { get; set; }
    
    [Required]
    [MaxLength(150)]
    public string Nome { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [MaxLength(150)]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string SenhaHash { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string Papel { get; set; } = "cliente"; // "admin" ou "cliente"
}