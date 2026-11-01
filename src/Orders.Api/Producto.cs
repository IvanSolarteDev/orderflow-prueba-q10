namespace Orders.Api;

// Catálogo mínimo: solo sirve para validar que el SKU del pedido exista.
public class Producto
{
    public string Sku { get; set; } = "";
}
