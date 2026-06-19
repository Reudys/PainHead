using System.ComponentModel.DataAnnotations;

namespace PainHead.Models;

public class Venta
{
    [Key]
    public int Id { get; set; }

    public int JornadaId { get; set; }

    public Jornada? Jornada { get; set; }

    public DateTime FechaVenta { get; set; } = DateTime.Now;

    public int NumeroMesa { get; set; }

    public string? NombreMesa { get; set; }

    public double Total { get; set; }
}