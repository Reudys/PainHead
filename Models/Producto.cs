using System.ComponentModel.DataAnnotations;

namespace PainHead.Models;

public class Producto
{
    [Key]
    [ScaffoldColumn(false)]
    public int Id { get; set; }

    [Required(ErrorMessage = "El nombre del Producto es Obligatorio")]
    public string Nombre { get; set; }

    [MaxLength(100)]
    public string? Descripcion { get; set; }

    [Required(ErrorMessage = "El Producto debe tener un Precio")]
    public double Precio { get; set; }

    public ICollection<PLista> PListas { get; set; } = new List<PLista>();
}