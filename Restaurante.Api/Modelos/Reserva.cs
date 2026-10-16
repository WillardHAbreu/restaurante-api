using System.ComponentModel.DataAnnotations;

namespace Restaurante.Api.Modelos;

public class Reserva
{
    public int Id { get; set; }

    public int MesaId { get; set; }
    public Mesa? Mesa { get; set; }

    public int UsuarioId { get; set; }
    public Usuario? Usuario { get; set; }

    public int NumeroPessoas { get; set; }

    // Fato técnico 2: O Npgsql exige DateTime com Kind Utc em "timestamp with time zone"
    private DateTime _dataHoraUtc;
    public DateTime DataHoraUtc
    {
        get => _dataHoraUtc;
        set
        {
            if (value.Kind != DateTimeKind.Utc)
            {
                throw new ArgumentException("A data da reserva deve estar em UTC (DateTimeKind.Utc).", nameof(value));
            }
            _dataHoraUtc = value;
        }
    }

    [MaxLength(50)]
    public string Status { get; set; } = "Confirmada"; // Confirmada, Cancelada
}