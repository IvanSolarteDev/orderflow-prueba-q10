using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace Orders.Api;

public class OrdersDbContext(DbContextOptions<OrdersDbContext> options) : DbContext(options)
{
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<Producto> Productos => Set<Producto>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Producto>().HasKey(p => p.Sku);

        // El estado se guarda como texto ("Pending") y no como número, para que sea legible en la base.
        modelBuilder.Entity<Order>().Property(o => o.Estado).HasConversion<string>();

        // Tablas del Outbox de MassTransit: guardan los eventos pendientes de publicar.
        modelBuilder.AddInboxStateEntity();
        modelBuilder.AddOutboxMessageEntity();
        modelBuilder.AddOutboxStateEntity();
    }

    // Carga el catálogo inicial si la tabla está vacía.
    public async Task SeedAsync()
    {
        if (await Productos.AnyAsync()) return;

        Productos.AddRange(
            new Producto { Sku = "ABC-01" },
            new Producto { Sku = "ABC-02" },
            new Producto { Sku = "ABC-03" });
        await SaveChangesAsync();
    }
}
