using Microsoft.EntityFrameworkCore;
using PainHead.Models;

namespace PainHead.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions options) : base(options)
    {
        
    }

    public DbSet<Producto> Productos { get; set; }
}
