using System.ComponentModel.DataAnnotations;

namespace PainHead.Models;

public class Jornada
{
    [Key]
    public int Id { get; set; }

    public DateTime FechaApertura { get; set; } = DateTime.Now;

    public DateTime? FechaCierre { get; set; }

    public bool Activa { get; set; } = true;

    public ICollection<Venta> Ventas { get; set; } = new List<Venta>();
}