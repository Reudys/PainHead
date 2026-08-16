using System.ComponentModel.DataAnnotations;

namespace PainHead.Models;

public class Pedido
{
    [Key]
    public int Id { get; set; }

    public int MesaId { get; set; }
    public Mesa? Mesa { get; set; }

    [Required]
    [MaxLength(100)]
    public string NombreCliente { get; set; } = string.Empty;

    public ICollection<PLista> PListas { get; set; } = new List<PLista>();

    public double Total =>
        PListas.Sum(p => p.PrecioUnitario * p.Cantidad);
}