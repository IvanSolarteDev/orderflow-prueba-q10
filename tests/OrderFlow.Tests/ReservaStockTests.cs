using Contracts;
using Inventory.Worker;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace OrderFlow.Tests;

public class ReservaStockTests : IDisposable
{
    // Base SQLite en memoria: es relacional de verdad (transacciones, claves), sin necesitar Postgres.
    private readonly SqliteConnection _conexion = new("DataSource=:memory:");
    private readonly InventoryDbContext _db;

    public ReservaStockTests()
    {
        _conexion.Open();
        _db = new InventoryDbContext(new DbContextOptionsBuilder<InventoryDbContext>().UseSqlite(_conexion).Options);
        _db.Database.EnsureCreated();
        _db.Stocks.Add(new Stock { Sku = "ABC-01", Disponible = 10 });
        _db.SaveChanges();
    }

    [Fact]
    public async Task Mismo_evento_procesado_dos_veces_descuenta_el_stock_una_sola_vez()
    {
        var reserva = new ReservaStock(_db);
        var evento = new OrderCreated(Guid.NewGuid(), Guid.NewGuid(), "ABC-01", 3, DateTime.UtcNow);

        var primera = await reserva.ReservarAsync(evento);
        var segunda = await reserva.ReservarAsync(evento);   // el mismo EventId llega otra vez

        Assert.True(primera);
        Assert.True(segunda);
        Assert.Equal(7, await _db.Stocks.Where(s => s.Sku == "ABC-01").Select(s => s.Disponible).SingleAsync());
    }

    [Fact]
    public async Task Sin_stock_suficiente_rechaza_y_no_descuenta()
    {
        var reserva = new ReservaStock(_db);
        var evento = new OrderCreated(Guid.NewGuid(), Guid.NewGuid(), "ABC-01", 11, DateTime.UtcNow);

        var reservado = await reserva.ReservarAsync(evento);

        Assert.False(reservado);
        Assert.Equal(10, await _db.Stocks.Where(s => s.Sku == "ABC-01").Select(s => s.Disponible).SingleAsync());
    }

    public void Dispose()
    {
        _db.Dispose();
        _conexion.Dispose();
    }
}
