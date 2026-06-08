using Microsoft.EntityFrameworkCore;
using PainHead.Models;

namespace PainHead.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    // DbSets (tablas)
    public DbSet<Producto> Productos { get; set; }
    public DbSet<Mesa> Mesas { get; set; }
    public DbSet<PLista> PListas { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ==================== CONFIGURACIÓN DE RELACIONES ====================

        modelBuilder.Entity<PLista>()
            .HasKey(pl => pl.Id);   // Clave primaria

        // Relación Mesa -> PLista (Uno a Muchos)
        modelBuilder.Entity<PLista>()
            .HasOne(pl => pl.Mesa)
            .WithMany(m => m.PListas)
            .HasForeignKey(pl => pl.MesaId)
            .OnDelete(DeleteBehavior.Cascade);   // Si eliminas una mesa, se eliminan sus items

        // Relación Producto -> PLista (Uno a Muchos)
        modelBuilder.Entity<PLista>()
            .HasOne(pl => pl.Producto)
            .WithMany(p => p.PListas)
            .HasForeignKey(pl => pl.ProductoId)
            .OnDelete(DeleteBehavior.Restrict);  // No permite borrar producto si está en una mesa
    }
}