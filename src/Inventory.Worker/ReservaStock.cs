using Contracts;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Worker;

// Lógica de negocio separada del consumidor para poder probarla con un test sin RabbitMQ.
public class ReservaStock(InventoryDbContext db)
{
    // Devuelve true si el stock quedó reservado, false si no había suficiente.
    public async Task<bool> ReservarAsync(OrderCreated evento)
    {
        // Todo ocurre en una sola transacción: o se guarda el descuento Y el evento procesado, o ninguno.
        await using var transaccion = await db.Database.BeginTransactionAsync();

        // Idempotencia: si este EventId ya se procesó, no se toca el stock y se devuelve el mismo resultado.
        var yaProcesado = await db.ProcessedEvents.FindAsync(evento.EventId);
        if (yaProcesado is not null) return yaProcesado.Reservado;

        // Descuento atómico: solo descuenta si hay suficiente. Devuelve cuántas filas cambió (0 o 1).
        var filas = await db.Stocks
            .Where(s => s.Sku == evento.Sku && s.Disponible >= evento.Cantidad)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.Disponible, x => x.Disponible - evento.Cantidad));
        var reservado = filas > 0;

        db.ProcessedEvents.Add(new ProcessedEvent { EventId = evento.EventId, Reservado = reservado });
        await db.SaveChangesAsync();
        await transaccion.CommitAsync();

        return reservado;
    }
}
