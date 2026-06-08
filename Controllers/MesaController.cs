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

    // GET: Mesa
    public async Task<IActionResult> Index()
    {
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

    // POST: Mesa/Close/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Close(int id)
    {
        var mesa = await _context.Mesas
            .Include(m => m.PListas)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (mesa == null) return NotFound();

        // Eliminar todos los productos de la mesa
        _context.PListas.RemoveRange(mesa.PListas);
        
        // Opcional: Eliminar la mesa completamente
        _context.Mesas.Remove(mesa);

        await _context.SaveChangesAsync();

        TempData["Success"] = $"Mesa {mesa.NumeroMesa} cerrada correctamente.";

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
}