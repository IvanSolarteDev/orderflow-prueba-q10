namespace Inventory.Worker;

public class Stock
{
    public string Sku { get; set; } = "";
    public int Disponible { get; set; }
}

// Un registro por cada evento ya procesado. Es la base de la idempotencia.
public class ProcessedEvent
{
    public Guid EventId { get; set; }
    public bool Reservado { get; set; }   // resultado que se dio: sirve si el evento llega repetido
}
