namespace Orders.Api;

public enum EstadoPedido
{
    Pending,
    Confirmed,
    Rejected
}

public class Order
{
    public Guid Id { get; private set; }
    public string ClienteNombre { get; private set; } = "";
    public string Sku { get; private set; } = "";
    public int Cantidad { get; private set; }
    public EstadoPedido Estado { get; private set; }
    public DateTime CreadoEn { get; private set; }

    // Constructor vacío: lo necesita EF Core para reconstruir el pedido desde la base de datos.
    private Order() { }

    public Order(string clienteNombre, string sku, int cantidad)
    {
        Id = Guid.NewGuid();
        ClienteNombre = clienteNombre;
        Sku = sku;
        Cantidad = cantidad;
        Estado = EstadoPedido.Pending;
        CreadoEn = DateTime.UtcNow;
    }

    // Regla de negocio: solo se puede pasar de Pending a Confirmed o a Rejected.
    public void Confirm()
    {
        if (Estado != EstadoPedido.Pending)
            throw new InvalidOperationException($"No se puede confirmar un pedido en estado {Estado}.");
        Estado = EstadoPedido.Confirmed;
    }

    public void Reject()
    {
        if (Estado != EstadoPedido.Pending)
            throw new InvalidOperationException($"No se puede rechazar un pedido en estado {Estado}.");
        Estado = EstadoPedido.Rejected;
    }
}
