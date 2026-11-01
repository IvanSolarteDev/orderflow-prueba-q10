using Contracts;
using MassTransit;

namespace Orders.Api;

// Cuando Inventory reserva el stock, el pedido pasa a Confirmed.
public class StockReservedConsumer(OrdersDbContext db) : IConsumer<StockReserved>
{
    public async Task Consume(ConsumeContext<StockReserved> context)
    {
        var order = await db.Orders.FindAsync(context.Message.OrderId);

        // Idempotencia: si no existe o ya no está en Pending, el evento se ignora.
        if (order is null || order.Estado != EstadoPedido.Pending) return;

        order.Confirm();
        await db.SaveChangesAsync();
    }
}

// Cuando Inventory rechaza por falta de stock, el pedido pasa a Rejected.
public class StockRejectedConsumer(OrdersDbContext db) : IConsumer<StockRejected>
{
    public async Task Consume(ConsumeContext<StockRejected> context)
    {
        var order = await db.Orders.FindAsync(context.Message.OrderId);

        if (order is null || order.Estado != EstadoPedido.Pending) return;

        order.Reject();
        await db.SaveChangesAsync();
    }
}
