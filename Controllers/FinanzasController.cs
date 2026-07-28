using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PainHead.Data;
using PainHead.Models;

namespace PainHead.Controllers;

public class FinanzasController : Controller
{
    private readonly AppDbContext _context;

    public FinanzasController(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(DateTime? desde, DateTime? hasta)
    {
        var ventas = _context.Ventas.AsQueryable();
        var gastos = _context.Gastos.AsQueryable();

        if (desde.HasValue)
        {
            ventas = ventas.Where(v => v.FechaVenta >= desde.Value);
            gastos = gastos.Where(g => g.Fecha >= desde.Value);
        }

        if (hasta.HasValue)
        {
            var fechaFinal = hasta.Value.AddDays(1);

            ventas = ventas.Where(v => v.FechaVenta < fechaFinal);
            gastos = gastos.Where(g => g.Fecha < fechaFinal);
        }

        ViewBag.Ingresos = await ventas.SumAsync(v => (double?)v.Total) ?? 0;

        ViewBag.Gastos = await gastos.SumAsync(g => (double?)g.Monto) ?? 0;

        ViewBag.Desde = desde;
        ViewBag.Hasta = hasta;

        return View(await gastos.OrderByDescending(g => g.Fecha).ToListAsync());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Crear(Gasto gasto)
    {
        if (!ModelState.IsValid)
            return RedirectToAction(nameof(Index));

        _context.Gastos.Add(gasto);

        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }
}