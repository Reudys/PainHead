using System.ComponentModel.DataAnnotations;

namespace PainHead.Models;

public class Mesa
{
    [Key]
    public int Id { get; set; }
    public int NumeroMesa { get; set; }
    public string? Nombre { get; set; }
    public double? TotalPagar { get; set; }

    public ICollection<PLista> PListas { get; set; } = new List<PLista>();
}