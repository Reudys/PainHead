using System.ComponentModel.DataAnnotations;

namespace PainHead.Models;

public class Gasto
{
    [Key]
    public int Id { get; set; }

    [Required(ErrorMessage = "Debe escribir un concepto.")]
    public string Concepto { get; set; } = string.Empty;

    [Required(ErrorMessage = "Debe indicar el monto.")]
    public double Monto { get; set; }

    public DateTime Fecha { get; set; } = DateTime.Now;
}