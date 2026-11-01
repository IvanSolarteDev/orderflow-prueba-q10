namespace Contracts;

// Eventos compartidos entre Orders.Api e Inventory.Worker.
// Son records (inmutables): un evento es un hecho que ya ocurrió y no se modifica.

public record OrderCreated(Guid EventId, Guid OrderId, string Sku, int Cantidad, DateTime OcurridoEn);

public record StockReserved(Guid EventId, Guid OrderId, DateTime OcurridoEn);

public record StockRejected(Guid EventId, Guid OrderId, string Motivo, DateTime OcurridoEn);
