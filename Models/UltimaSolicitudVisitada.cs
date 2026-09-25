namespace PlataformaCreditos.Models;

/// <summary>Información de la última solicitud visitada, persistida en sesión como JSON.</summary>
public class UltimaSolicitudVisitada
{
    public int SolicitudId { get; set; }

    public decimal MontoSolicitado { get; set; }
}