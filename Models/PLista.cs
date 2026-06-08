using System.ComponentModel.DataAnnotations;

namespace PainHead.Models;

public class PLista
{
    [Key]
    public int Id { get; set; }                    // Clave primaria de la relación

    public int MesaId { get; set; }                // Clave foránea hacia Mesa
    public int ProductoId { get; set; }            // Clave foránea hacia Producto

    // Propiedades de navegación (muy importantes)
    public Mesa? Mesa { get; set; }
    public Producto? Producto { get; set; }

    // Datos adicionales útiles (recomendado)
    public int Cantidad { get; set; } = 1;
    public double PrecioUnitario { get; set; }    // Para guardar el precio al momento de agregar
}