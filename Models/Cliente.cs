using System.ComponentModel.DataAnnotations;

namespace PlataformaCreditos.Models;

public class Cliente
{
    public int Id { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;

    [Range(0.01, double.MaxValue, ErrorMessage = "Los ingresos mensuales deben ser mayores a 0.")]
    [DataType(DataType.Currency)]
    [Display(Name = "Ingresos mensuales")]
    public decimal IngresosMensuales { get; set; }

    public bool Activo { get; set; } = true;
}