using Microsoft.EntityFrameworkCore;

namespace Inventory.Worker;

public class InventoryDbContext(DbContextOptions<InventoryDbContext> options) : DbContext(options)
{
    public DbSet<Stock> Stocks => Set<Stock>();
    public DbSet<ProcessedEvent> ProcessedEvents => Set<ProcessedEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Stock>().HasKey(s => s.Sku);
        modelBuilder.Entity<ProcessedEvent>().HasKey(e => e.EventId); // clave única: no puede repetirse
    }

    // Carga el stock inicial si la tabla está vacía.
    public async Task SeedAsync()
    {
        if (await Stocks.AnyAsync()) return;

        Stocks.AddRange(
            new Stock { Sku = "ABC-01", Disponible = 100 },
            new Stock { Sku = "ABC-02", Disponible = 50 },
            new Stock { Sku = "ABC-03", Disponible = 5 });
        await SaveChangesAsync();
    }
}
