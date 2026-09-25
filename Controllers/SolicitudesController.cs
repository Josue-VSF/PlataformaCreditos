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
    private const decimal MultiploIngresos = 10m;

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
        // Los montos de un rango nunca pueden ser negativos.
        if (montoMin.HasValue && montoMin.Value < 0)
        {
            ModelState.AddModelError(nameof(montoMin), "El monto mínimo no puede ser negativo.");
        }

        if (montoMax.HasValue && montoMax.Value < 0)
        {
            ModelState.AddModelError(nameof(montoMax), "El monto máximo no puede ser negativo.");
        }

        // El inicio de un rango de fechas no puede ser posterior al fin.
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

    // GET: Solicitudes/CompletarPerfil
    public async Task<IActionResult> CompletarPerfil()
    {
        var userId = _userManager.GetUserId(User);

        // Si el cliente ya existe, no hace falta volver a completar el perfil.
        var existe = await _context.Clientes.AsNoTracking().AnyAsync(c => c.UserId == userId);
        if (existe)
        {
            return RedirectToAction(nameof(Crear));
        }

        return View(new Cliente());
    }

    // POST: Solicitudes/CompletarPerfil
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CompletarPerfil([Bind(nameof(Cliente.IngresosMensuales))] Cliente cliente)
    {
        var userId = _userManager.GetUserId(User);

        // Validación server-side: ingresos mensuales > 0 (también reforzada por [Range]).
        if (!ModelState.IsValid)
        {
            return View(cliente);
        }

        var existe = await _context.Clientes.AsNoTracking().AnyAsync(c => c.UserId == userId);
        if (existe)
        {
            return RedirectToAction(nameof(Crear));
        }

        _context.Clientes.Add(new Cliente
        {
            UserId = userId!,
            IngresosMensuales = cliente.IngresosMensuales,
            Activo = true
        });
        await _context.SaveChangesAsync();

        TempData["Mensaje"] = "Perfil completado correctamente. Ya puedes crear solicitudes.";
        return RedirectToAction(nameof(Crear));
    }

    // GET: Solicitudes/Crear
    public async Task<IActionResult> Crear()
    {
        var userId = _userManager.GetUserId(User);

        var cliente = await _context.Clientes
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.UserId == userId);

        // i) Sin cliente: primero debe completar el perfil.
        if (cliente == null)
        {
            return RedirectToAction(nameof(CompletarPerfil));
        }

        // ii) Cliente inactivo: muestra la vista con error y sin formulario habilitado.
        if (!cliente.Activo)
        {
            ViewBag.PuedeCrear = false;
            TempData["Error"] = "Tu perfil está inactivo. Contacta con el administrador para poder crear solicitudes.";
            return View(new Solicitud());
        }

        ViewBag.PuedeCrear = true;
        return View(new Solicitud());
    }

    // POST: Solicitudes/Crear
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Crear(
        [Bind(nameof(Solicitud.NombreCliente), nameof(Solicitud.MontoSolicitado),
              nameof(Solicitud.PlazoMeses), nameof(Solicitud.Finalidad),
              nameof(Solicitud.Observaciones))] Solicitud solicitud)
    {
        var userId = _userManager.GetUserId(User);

        // b) El cliente asociado debe existir y estar Activo.
        var cliente = await _context.Clientes
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.UserId == userId);

        if (cliente == null)
        {
            TempData["Error"] = "Debes completar tu perfil de cliente antes de crear una solicitud.";
            return RedirectToAction(nameof(CompletarPerfil));
        }

        if (!cliente.Activo)
        {
            ViewBag.PuedeCrear = false;
            TempData["Error"] = "Tu perfil está inactivo. Contacta con el administrador para poder crear solicitudes.";
            return View(solicitud);
        }

        // c) No debe existir otra solicitud en estado Pendiente.
        var tienePendiente = await _context.Solicitudes
            .AsNoTracking()
            .AnyAsync(s => s.UserId == userId && s.Estado == "Pendiente");

        if (tienePendiente)
        {
            TempData["Error"] = "Ya tienes una solicitud en estado Pendiente. Debes esperar a que sea procesada antes de crear otra.";
            return View(solicitud);
        }

        // d) El monto no puede ser negativo ni cero.
        if (solicitud.MontoSolicitado <= 0)
        {
            TempData["Error"] = "El monto solicitado debe ser mayor a 0.";
            ModelState.AddModelError(nameof(Solicitud.MontoSolicitado), "El monto solicitado debe ser mayor a 0.");
            return View(solicitud);
        }

        // e) El monto no puede superar 10 veces los ingresos mensuales del cliente.
        var montoMaximo = MultiploIngresos * cliente.IngresosMensuales;
        if (solicitud.MontoSolicitado > montoMaximo)
        {
            TempData["Error"] = $"El monto solicitado supera 10 veces tus ingresos mensuales ({montoMaximo.ToString("C")}).";
            ModelState.AddModelError(nameof(Solicitud.MontoSolicitado), $"El monto no puede superar 10 veces tus ingresos mensuales ({montoMaximo.ToString("C")}).");
            return View(solicitud);
        }

        _context.Solicitudes.Add(new Solicitud
        {
            UserId = userId!,
            NombreCliente = solicitud.NombreCliente,
            MontoSolicitado = solicitud.MontoSolicitado,
            PlazoMeses = solicitud.PlazoMeses,
            Finalidad = solicitud.Finalidad,
            Observaciones = solicitud.Observaciones,
            Estado = "Pendiente",
            FechaSolicitud = DateTime.Now
        });
        await _context.SaveChangesAsync();

        TempData["Mensaje"] = "Solicitud creada correctamente.";
        return RedirectToAction(nameof(Index));
    }
}