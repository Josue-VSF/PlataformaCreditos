using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PlataformaCreditos.Data;
using PlataformaCreditos.Models;

namespace PlataformaCreditos.Controllers;

[Authorize]
public class SolicitudesController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<IdentityUser> _userManager;

    public SolicitudesController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    // GET: Solicitudes/Index?estado=&montoMin=&montoMax=&fechaInicio=&fechaFin=
    public async Task<IActionResult> Index(
        string? estado,
        decimal? montoMin,
        decimal? montoMax,
        DateTime? fechaInicio,
        DateTime? fechaFin)
    {
        // Invalidante: los montos de un rango nunca pueden ser negativos.
        if (montoMin.HasValue && montoMin.Value < 0)
        {
            ModelState.AddModelError(nameof(montoMin), "El monto mínimo no puede ser negativo.");
        }

        if (montoMax.HasValue && montoMax.Value < 0)
        {
            ModelState.AddModelError(nameof(montoMax), "El monto máximo no puede ser negativo.");
        }

        // Invalidante: el inicio de un rango de fechas no puede ser posterior al fin.
        if (fechaInicio.HasValue && fechaFin.HasValue && fechaInicio.Value > fechaFin.Value)
        {
            ModelState.AddModelError(nameof(fechaFin), "La fecha de inicio no puede ser posterior a la fecha de fin.");
        }

        var userId = _userManager.GetUserId(User);

        var query = _context.Solicitudes
            .AsNoTracking()
            .Where(s => s.UserId == userId);

        // Filtro por Estado (válido siempre).
        if (!string.IsNullOrWhiteSpace(estado))
        {
            query = query.Where(s => s.Estado == estado);
        }

        // Filtros de rango: solo se aplican si las validaciones pasaron.
        if (ModelState.IsValid)
        {
            if (montoMin.HasValue)
            {
                query = query.Where(s => s.MontoSolicitado >= montoMin.Value);
            }

            if (montoMax.HasValue)
            {
                query = query.Where(s => s.MontoSolicitado <= montoMax.Value);
            }

            if (fechaInicio.HasValue)
            {
                query = query.Where(s => s.FechaSolicitud.Date >= fechaInicio.Value.Date);
            }

            if (fechaFin.HasValue)
            {
                query = query.Where(s => s.FechaSolicitud.Date <= fechaFin.Value.Date);
            }
        }

        var solicitudes = await query
            .OrderByDescending(s => s.FechaSolicitud)
            .ToListAsync();

        // Se preservan los filtros para re-renderizar el formulario.
        ViewBag.Estado = estado;
        ViewBag.MontoMin = montoMin;
        ViewBag.MontoMax = montoMax;
        ViewBag.FechaInicio = fechaInicio?.ToString("yyyy-MM-dd");
        ViewBag.FechaFin = fechaFin?.ToString("yyyy-MM-dd");

        return View(solicitudes);
    }

    // GET: Solicitudes/Detalle/5
    public async Task<IActionResult> Detalle(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var userId = _userManager.GetUserId(User);

        var solicitud = await _context.Solicitudes
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == id && s.UserId == userId);

        if (solicitud == null)
        {
            return NotFound();
        }

        return View(solicitud);
    }
}