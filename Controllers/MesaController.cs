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

    public async Task<IActionResult> Index()
    {
        var jornadaActiva = await _context.Jornadas
            .FirstOrDefaultAsync(j => j.Activa);

        if (jornadaActiva == null)
            return RedirectToAction(nameof(Start));

        var mesas = await _context.Mesas
            .Include(m => m.Pedidos)
                .ThenInclude(p => p.PListas)
            .Include(m => m.PListas)
                .ThenInclude(pl => pl.Producto)
            .ToListAsync();

        return View(mesas);
    }

    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
            return NotFound();

        var mesa = await _context.Mesas
            .Include(m => m.Pedidos)
                .ThenInclude(p => p.PListas)
                    .ThenInclude(pl => pl.Producto)
            .Include(m => m.PListas)
                .ThenInclude(pl => pl.Producto)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (mesa == null)
            return NotFound();

        return View(mesa);
    }

    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Nombre")] Mesa mesa)
    {
        if (ModelState.IsValid)
        {
            var ultimoNumero = await _context.Mesas
                .OrderByDescending(m => m.NumeroMesa)
                .Select(m => m.NumeroMesa)
                .FirstOrDefaultAsync();

            mesa.NumeroMesa = ultimoNumero + 1;

            _context.Mesas.Add(mesa);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        return View(mesa);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CrearPedido(int mesaId, string nombreCliente)
    {
        if (string.IsNullOrWhiteSpace(nombreCliente))
        {
            TempData["Error"] = "Debes indicar el nombre del cliente.";

            return RedirectToAction(nameof(Details), new { id = mesaId });
        }

        var mesa = await _context.Mesas.FindAsync(mesaId);

        if (mesa == null)
            return NotFound();

        var pedido = new Pedido
        {
            MesaId = mesaId,
            NombreCliente = nombreCliente.Trim()
        };

        _context.Pedidos.Add(pedido);
        await _context.SaveChangesAsync();

        TempData["Success"] =
            $"Pedido de {pedido.NombreCliente} creado correctamente.";

        return RedirectToAction(nameof(Details), new { id = mesaId });
    }

    public async Task<IActionResult> AddProducto(int? pedidoId)
    {
        if (pedidoId == null)
            return NotFound();

        var pedido = await _context.Pedidos
            .Include(p => p.Mesa)
            .FirstOrDefaultAsync(p => p.Id == pedidoId);

        if (pedido == null)
            return NotFound();

        var productos = await _context.Productos
            .OrderBy(p => p.Nombre)
            .ToListAsync();

        ViewBag.Productos = productos;

        return View(pedido);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddProducto(
        int pedidoId,
        int productoId,
        int cantidad = 1,
        string? especificacion = null)
    {
        if (cantidad < 1)
            cantidad = 1;

        var pedido = await _context.Pedidos
            .Include(p => p.Mesa)
            .FirstOrDefaultAsync(p => p.Id == pedidoId);

        if (pedido == null)
            return NotFound();

        var producto = await _context.Productos
            .FindAsync(productoId);

        if (producto == null)
            return NotFound();

        var pLista = new PLista
        {
            MesaId = pedido.MesaId,
            PedidoId = pedido.Id,
            ProductoId = producto.Id,
            Cantidad = cantidad,
            PrecioUnitario = producto.Precio,
            Especificacion = string.IsNullOrWhiteSpace(especificacion)
                ? null
                : especificacion.Trim()
        };

        _context.PListas.Add(pLista);

        await _context.SaveChangesAsync();

        await ActualizarTotalMesa(pedido.MesaId);

        return RedirectToAction(
            nameof(Details),
            new { id = pedido.MesaId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveProducto(int id)
    {
        var pLista = await _context.PListas
            .FirstOrDefaultAsync(p => p.Id == id);

        if (pLista == null)
            return NotFound();

        var mesaId = pLista.MesaId;

        _context.PListas.Remove(pLista);
        await _context.SaveChangesAsync();

        await ActualizarTotalMesa(mesaId);

        return RedirectToAction(
            nameof(Details),
            new { id = mesaId });
    }

    public async Task<IActionResult> ConfirmClose(int? id)
    {
        if (id == null)
            return NotFound();

        var mesa = await _context.Mesas
            .Include(m => m.Pedidos)
                .ThenInclude(p => p.PListas)
                    .ThenInclude(pl => pl.Producto)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (mesa == null)
            return NotFound();

        return View(mesa);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Close(int id)
    {
        var mesa = await _context.Mesas
            .Include(m => m.Pedidos)
                .ThenInclude(p => p.PListas)
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

        var totalMesa = mesa.Pedidos
            .SelectMany(p => p.PListas)
            .Sum(p => p.PrecioUnitario * p.Cantidad);

        var venta = new Venta
        {
            JornadaId = jornada.Id,
            FechaVenta = DateTime.Now,
            NumeroMesa = mesa.NumeroMesa,
            NombreMesa = mesa.Nombre,
            Total = totalMesa
        };

        _context.Ventas.Add(venta);
        await _context.SaveChangesAsync();

        foreach (var pedido in mesa.Pedidos)
        {
            foreach (var item in pedido.PListas)
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
                    NombreCliente = pedido.NombreCliente,
                    Cantidad = item.Cantidad,
                    PrecioUnitario = item.PrecioUnitario,
                    SubTotal = item.Cantidad * item.PrecioUnitario
                };

                _context.ProductosVendidos.Add(productoVendido);
            }
        }

        _context.PListas.RemoveRange(
            mesa.Pedidos.SelectMany(p => p.PListas));

        _context.Pedidos.RemoveRange(mesa.Pedidos);
        _context.Mesas.Remove(mesa);

        await _context.SaveChangesAsync();

        TempData["Success"] =
            $"Mesa {mesa.NumeroMesa} cerrada correctamente. " +
            $"Venta registrada por RD$ {venta.Total:N2}.";

        return RedirectToAction(nameof(Index));
    }

    private async Task ActualizarTotalMesa(int mesaId)
    {
        var mesa = await _context.Mesas
            .Include(m => m.Pedidos)
                .ThenInclude(p => p.PListas)
            .FirstOrDefaultAsync(m => m.Id == mesaId);

        if (mesa == null)
            return;

        mesa.TotalPagar = mesa.Pedidos
            .SelectMany(p => p.PListas)
            .Sum(p => p.PrecioUnitario * p.Cantidad);

        await _context.SaveChangesAsync();
    }

    public async Task<IActionResult> AbrirJornada()
    {
        var existe = await _context.Jornadas
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

    public async Task<IActionResult> Jornada(
        DateTime? desde,
        DateTime? hasta)
    {
        var jornadas = _context.Jornadas
            .Include(j => j.Ventas)
            .OrderByDescending(j => j.FechaApertura)
            .AsQueryable();

        if (desde.HasValue)
        {
            jornadas = jornadas.Where(j =>
                j.FechaApertura.Date >= desde.Value.Date);
        }

        if (hasta.HasValue)
        {
            jornadas = jornadas.Where(j =>
                j.FechaApertura.Date <= hasta.Value.Date);
        }

        var lista = await jornadas.ToListAsync();

        ViewBag.GananciasTotales = lista
            .Sum(j => j.Ventas.Sum(v => v.Total));

        ViewBag.Desde = desde;
        ViewBag.Hasta = hasta;

        return View(lista);
    }

    public async Task<IActionResult> DetalleJornada(int id)
    {
        var jornada = await _context.Jornadas
            .Include(j => j.Ventas)
                .ThenInclude(v => v.ProductosVendidos)
            .FirstOrDefaultAsync(j => j.Id == id);

        if (jornada == null)
            return NotFound();

        ViewBag.GananciasTotales =
            await _context.Ventas
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