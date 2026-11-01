using Contracts;
using MassTransit;

namespace Inventory.Worker;

// Recibe OrderCreated, pide a ReservaStock que descuente y publica la respuesta.
public class OrderCreatedConsumer(ReservaStock reserva) : IConsumer<OrderCreated>
{
    public async Task Consume(ConsumeContext<OrderCreated> context)
    {
        var evento = context.Message;
        var reservado = await reserva.ReservarAsync(evento);

        if (reservado)
            await context.Publish(new StockReserved(Guid.NewGuid(), evento.OrderId, DateTime.UtcNow));
        else
            await context.Publish(new StockRejected(Guid.NewGuid(), evento.OrderId, "Stock insuficiente", DateTime.UtcNow));
    }
}
