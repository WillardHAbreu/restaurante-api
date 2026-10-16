using System.ComponentModel.DataAnnotations;

namespace Restaurante.Api.Modelos;

public class Mesa
{
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string NumeroOuNome { get; set; } = string.Empty; // Ex: "Mesa 01", "Varanda 2"

    [Range(1, 20)]
    public int CapacidadePessoas { get; set; }

    [MaxLength(200)]
    public string Localizacao { get; set; } = string.Empty; // Ex: Salão principal, Área externa

    // Relação 1 para N com Reserva
    public ICollection<Reserva> Reservas { get; set; } = new List<Reserva>();
}