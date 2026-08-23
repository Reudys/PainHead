using Microsoft.EntityFrameworkCore;
using PainHead.Data;
using System.Net;
using System.Net.Sockets;

var builder = WebApplication.CreateBuilder(args);

// Escuchar en todas las interfaces de red
builder.WebHost.UseUrls("http://0.0.0.0:5046");

builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite("Data Source=Data/painhead.db"));

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// No usar HTTPS para esta versión de red local
// app.UseHttpsRedirection();

app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Mesa}/{action=Start}/{id?}")
    .WithStaticAssets();


// ========================================
// MOSTRAR DIRECCIÓN DE ACCESO
// ========================================

Console.WriteLine();
Console.WriteLine("========================================");
Console.WriteLine("              PAINHEAD");
Console.WriteLine("========================================");
Console.WriteLine();
Console.WriteLine("Servidor iniciado correctamente.");
Console.WriteLine();
Console.WriteLine("💻 Esta computadora:");
Console.WriteLine("   http://localhost:5046");
Console.WriteLine();

string? ipLocal = ObtenerIPLocal();

if (ipLocal != null)
{
    Console.WriteLine("📱 Celular / Tablet:");
    Console.WriteLine($"   http://{ipLocal}:5046");
}
else
{
    Console.WriteLine("📱 No se pudo detectar la IP local.");
}

Console.WriteLine();
Console.WriteLine("⚠️ Los dispositivos deben estar conectados");
Console.WriteLine("   a la misma red Wi-Fi.");
Console.WriteLine();
Console.WriteLine("========================================");
Console.WriteLine();

app.Run();


// ========================================
// OBTENER IP LOCAL
// ========================================

static string? ObtenerIPLocal()
{
    try
    {
        using var socket = new Socket(
            AddressFamily.InterNetwork,
            SocketType.Dgram,
            0);

        socket.Connect("8.8.8.8", 65530);

        if (socket.LocalEndPoint is IPEndPoint endpoint)
        {
            return endpoint.Address.ToString();
        }
    }
    catch
    {
        // Si no se puede detectar la IP
    }

    return null;
}