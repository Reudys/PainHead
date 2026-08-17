using System.ComponentModel.DataAnnotations;

namespace PainHead.Models;

public class PLista
{
    [Key]
    public int Id { get; set; }

    public int MesaId { get; set; }
    public Mesa? Mesa { get; set; }

    public int PedidoId { get; set; }
    public Pedido? Pedido { get; set; }

    public int ProductoId { get; set; }
    public Producto? Producto { get; set; }

    public int Cantidad { get; set; } = 1;

    public double PrecioUnitario { get; set; }

    [MaxLength(250)]
    public string? Especificacion { get; set; }
}