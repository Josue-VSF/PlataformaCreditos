using System.ComponentModel.DataAnnotations;

namespace PlataformaCreditos.Models;

public class Solicitud
{
    public int Id { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;

    [Required]
    [StringLength(120)]
    [Display(Name = "Nombre del cliente")]
    public string NombreCliente { get; set; } = string.Empty;

    [DataType(DataType.Currency)]
    [Display(Name = "Monto solicitado")]
    public decimal MontoSolicitado { get; set; }

    [Range(1, 360, ErrorMessage = "El plazo debe ser de entre 1 y 360 meses.")]
    [Display(Name = "Plazo (meses)")]
    public int PlazoMeses { get; set; }

    [StringLength(200)]
    [Display(Name = "Finalidad")]
    public string? Finalidad { get; set; }

    [Required]
    [StringLength(20)]
    [Display(Name = "Estado")]
    public string Estado { get; set; } = "Pendiente";

    [Display(Name = "Fecha de solicitud")]
    public DateTime FechaSolicitud { get; set; } = DateTime.Now;

    [StringLength(1000)]
    [DataType(DataType.MultilineText)]
    [Display(Name = "Observaciones")]
    public string? Observaciones { get; set; }
}