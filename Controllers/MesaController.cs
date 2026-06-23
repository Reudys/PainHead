using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PainHead.Data;
using PainHead.Models;

namespace PainHead.Controllers;

public class MesaController : Controller
{
    private readonly AppDbContext _context;

    public MesaController(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Start()
    {
        var jornadaActiva = await _context.Jornadas
            .FirstOrDefaultAsync(j => j.Activa);

        return View(jornadaActiva);
    }

    // GET: Mesa
    public async Task<IActionResult> Index()
    {
        var jornadaActiva = await _context.Jornadas
            .FirstOrDefaultAsync(j => j.Activa);

        if (jornadaActiva == null)
            return RedirectToAction(nameof(Start));

        var mesas = await _context.Mesas
            .Include(m => m.PListas)
            .ThenInclude(pl => pl.Producto)
            .ToListAsync();

        return View(mesas);
    }

    // GET: Mesa/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null) return NotFound();

        var mesa = await _context.Mesas
            .Include(m => m.PListas)
                .ThenInclude(pl => pl.Producto)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (mesa == null) return NotFound();

        return View(mesa);
    }

    // GET: Mesa/Create
    public IActionResult Create()
    {
        return View();
    }

    // POST: Mesa/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Nombre")] Mesa mesa)
    {
        if (ModelState.IsValid)
        {
            // Lógica para NumeroMesa auto-incrementado
            var ultimoNumero = await _context.Mesas
                .OrderByDescending(m => m.NumeroMesa)
                .Select(m => m.NumeroMesa)
                .FirstOrDefaultAsync();

            mesa.NumeroMesa = ultimoNumero + 1;

            _context.Add(mesa);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }
        return View(mesa);
    }

    // GET: Mesa/AddProducto/5   → Agregar producto a una mesa
    public async Task<IActionResult> AddProducto(int? id)
    {
        if (id == null) return NotFound();

        var mesa = await _context.Mesas.FindAsync(id);
        if (mesa == null) return NotFound();

        ViewBag.Productos = await _context.Productos.ToListAsync();
        ViewBag.MesaId = id;

        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddProducto(int mesaId, int productoId, int cantidad = 1)
    {
        var mesa = await _context.Mesas.FindAsync(mesaId);
        var producto = await _context.Productos.FindAsync(productoId);

        if (mesa == null || producto == null)
            return NotFound();

        var pLista = new PLista
        {
            MesaId = mesaId,
            ProductoId = productoId,
            Cantidad = cantidad,
            PrecioUnitario = producto.Precio
        };

        _context.PListas.Add(pLista);
        await _context.SaveChangesAsync();

        await ActualizarTotalMesa(mesaId);   // ← Actualiza el total

        return RedirectToAction(nameof(Details), new { id = mesaId });
    }

    // POST: Mesa/RemoveProducto/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveProducto(int id)
    {
        var pLista = await _context.PListas.FindAsync(id);
        if (pLista == null)
            return NotFound();

        int mesaId = pLista.MesaId;

        _context.PListas.Remove(pLista);
        await _context.SaveChangesAsync();

        await ActualizarTotalMesa(mesaId);   // ← Actualiza el total

        return RedirectToAction(nameof(Details), new { id = mesaId });
    }

    // GET: Mesa/ConfirmClose/5
    public async Task<IActionResult> ConfirmClose(int? id)
    {
        if (id == null) return NotFound();

        var mesa = await _context.Mesas
            .Include(m => m.PListas)
                .ThenInclude(pl => pl.Producto)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (mesa == null) return NotFound();

        return View(mesa);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Close(int id)
    {
        var mesa = await _context.Mesas
            .Include(m => m.PListas)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (mesa == null)
            return NotFound();

        var jornada = await _context.Jornadas
            .FirstOrDefaultAsync(j => j.Activa);

        if (jornada == null)
        {
            TempData["Error"] = "No existe una jornada activa.";
            return RedirectToAction(nameof(Index));
        }

        var venta = new Venta
        {
            JornadaId = jornada.Id,
            FechaVenta = DateTime.Now,
            NumeroMesa = mesa.NumeroMesa,
            NombreMesa = mesa.Nombre,
            Total = mesa.TotalPagar ?? 0
        };

        _context.Ventas.Add(venta);

        await _context.SaveChangesAsync();

        foreach (var item in mesa.PListas)
        {
            var producto = await _context.Productos
                .FirstOrDefaultAsync(p => p.Id == item.ProductoId);

            if (producto == null)
                continue;

            var productoVendido = new ProductoVendido
            {
                VentaId = venta.Id,
                ProductoId = producto.Id,
                NombreProducto = producto.Nombre,
                Cantidad = item.Cantidad,
                PrecioUnitario = item.PrecioUnitario,
                SubTotal = item.Cantidad * item.PrecioUnitario
            };

            _context.ProductosVendidos.Add(productoVendido);
        }

        _context.PListas.RemoveRange(mesa.PListas);

        _context.Mesas.Remove(mesa);

        await _context.SaveChangesAsync();

        TempData["Success"] =
            $"Mesa {mesa.NumeroMesa} cerrada correctamente. Venta registrada por RD$ {venta.Total:N2}.";

        return RedirectToAction(nameof(Index));
    }

    // Método auxiliar para recalcular el total
    private async Task ActualizarTotalMesa(int mesaId)
    {
        var mesa = await _context.Mesas
            .Include(m => m.PListas)
            .FirstOrDefaultAsync(m => m.Id == mesaId);

        if (mesa == null) return;

        mesa.TotalPagar = mesa.PListas.Sum(pl => pl.PrecioUnitario * pl.Cantidad);

        await _context.SaveChangesAsync();
    }

    public async Task<IActionResult> AbrirJornada()
    {
        bool existe = await _context.Jornadas
            .AnyAsync(j => j.Activa);

        if (!existe)
        {
            var jornada = new Jornada();

            _context.Jornadas.Add(jornada);

            await _context.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Start));
    }

    public async Task<IActionResult> CerrarJornada()
    {
        var jornada = await _context.Jornadas
            .FirstOrDefaultAsync(j => j.Activa);

        if (jornada != null)
        {
            jornada.Activa = false;
            jornada.FechaCierre = DateTime.Now;

            await _context.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Start));
    }

    public async Task<IActionResult> Jornada()
    {
        var jornadas = await _context.Jornadas
            .Include(j => j.Ventas)
            .OrderByDescending(j => j.FechaApertura)
            .ToListAsync();

        ViewBag.GananciasTotales = await _context.Ventas
            .SumAsync(v => (double?)v.Total) ?? 0;

        return View(jornadas);
    }

    public async Task<IActionResult> DetalleJornada(int id)
    {
        var jornada = await _context.Jornadas
            .Include(j => j.Ventas)
            .FirstOrDefaultAsync(j => j.Id == id);

        if (jornada == null)
            return NotFound();

        ViewBag.GananciasTotales = await _context.Ventas
            .SumAsync(v => (double?)v.Total) ?? 0;

        return View(jornada);
    }

    public async Task<IActionResult> ProductosVendidos()
    {
        var productos = await _context.ProductosVendidos
            .GroupBy(p => p.NombreProducto)
            .Select(g => new ProductoVendidoResumen
            {
                Nombre = g.Key,
                CantidadVendida = g.Sum(x => x.Cantidad),
                Ingresos = g.Sum(x => x.SubTotal)
            })
            .OrderByDescending(x => x.CantidadVendida)
            .ToListAsync();

        return View(productos);
    }
}