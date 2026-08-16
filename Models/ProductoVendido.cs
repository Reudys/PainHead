using System.ComponentModel.DataAnnotations;

namespace PainHead.Models;

public class ProductoVendido
{
    [Key]
    public int Id { get; set; }

    public int VentaId { get; set; }
    public Venta? Venta { get; set; }

    public int ProductoId { get; set; }

    public string NombreProducto { get; set; } = string.Empty;

    public string NombreCliente { get; set; } = string.Empty;

    public int Cantidad { get; set; }

    public double PrecioUnitario { get; set; }

    public double SubTotal { get; set; }
}