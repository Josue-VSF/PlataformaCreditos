using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using PlataformaCreditos.Data;
using PlataformaCreditos.Models;

namespace PlataformaCreditos.Controllers;

[Authorize(Roles = "Analista")]
public class AnalistaController : Controller
{
    private const decimal MaxVecesIngresos = 5m;

    private readonly ApplicationDbContext _context;
    private readonly IDistributedCache _cache;

    public AnalistaController(ApplicationDbContext context, IDistributedCache cache)
    {
        _context = context;
        _cache = cache;
    }

    // GET: Analista/Index
    public async Task<IActionResult> Index()
    {
        // Solicitudes Pendientes con el cliente asociado (join por UserId).
        var pendientes = await (from s in _context.Solicitudes.AsNoTracking()
                                join c in _context.Clientes.AsNoTracking() on s.UserId equals c.UserId into grupoCliente
                                from cliente in grupoCliente.DefaultIfEmpty()
                                where s.Estado == "Pendiente"
                                orderby s.FechaSolicitud ascending
                                select new SolicitudPendienteViewModel
                                {
                                    Solicitud = s,
                                    IngresosMensuales = cliente != null ? cliente.IngresosMensuales : 0m
                                }).ToListAsync();

        return View(pendientes);
    }

    // POST: Analista/Aprobar/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Aprobar(int id)
    {
        var solicitud = await _context.Solicitudes.FirstOrDefaultAsync(s => s.Id == id);

        if (solicitud == null)
        {
            TempData["Error"] = "La solicitud no existe.";
            return RedirectToAction(nameof(Index));
        }

        if (solicitud.Estado != "Pendiente")
        {
            TempData["Error"] = $"La solicitud #{id} ya no está en estado Pendiente, no se puede aprobar.";
            return RedirectToAction(nameof(Index));
        }

        var cliente = await _context.Clientes
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.UserId == solicitud.UserId);

        if (cliente == null)
        {
            TempData["Error"] = $"No se encontró el cliente asociado a la solicitud #{id}. No se puede aprobar.";
            return RedirectToAction(nameof(Index));
        }

        var maximoAprobable = MaxVecesIngresos * cliente.IngresosMensuales;
        if (solicitud.MontoSolicitado > maximoAprobable)
        {
            TempData["Error"] = $"No se puede aprobar la solicitud #{id}: el monto ({solicitud.MontoSolicitado.ToString("C")}) supera 5 veces los ingresos mensuales del cliente ({maximoAprobable.ToString("C")}).";
            return RedirectToAction(nameof(Index));
        }

        solicitud.Estado = "Aprobado";

        // Primero se persiste el cambio en la base de datos...
        await _context.SaveChangesAsync();

        // ...y después se invalida el cache Redis del listado del usuario dueño.
        await InvalidarCacheSolicitudes(solicitud.UserId);

        TempData["Mensaje"] = $"Solicitud #{id} aprobada correctamente.";
        return RedirectToAction(nameof(Index));
    }

    // POST: Analista/Rechazar/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Rechazar(int id, string? motivoRechazo)
    {
        var solicitud = await _context.Solicitudes.FirstOrDefaultAsync(s => s.Id == id);

        if (solicitud == null)
        {
            TempData["Error"] = "La solicitud no existe.";
            return RedirectToAction(nameof(Index));
        }

        if (solicitud.Estado != "Pendiente")
        {
            TempData["Error"] = $"La solicitud #{id} ya no está en estado Pendiente, no se puede rechazar.";
            return RedirectToAction(nameof(Index));
        }

        if (string.IsNullOrWhiteSpace(motivoRechazo))
        {
            TempData["Error"] = "El motivo de rechazo es obligatorio.";
            return RedirectToAction(nameof(Index));
        }

        solicitud.Estado = "Rechazado";
        solicitud.MotivoRechazo = motivoRechazo.Trim();

        await _context.SaveChangesAsync();

        await InvalidarCacheSolicitudes(solicitud.UserId);

        TempData["Mensaje"] = $"Solicitud #{id} rechazada correctamente.";
        return RedirectToAction(nameof(Index));
    }

    // Invalida el listado cacheado en Redis del usuario dueño de la solicitud.
    private async Task InvalidarCacheSolicitudes(string userId)
    {
        await _cache.RemoveAsync($"solicitudes:{userId}");
    }
}