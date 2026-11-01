namespace Orders.Api;

public record CrearPedidoRequest(string? ClienteNombre, string? Sku, int Cantidad);

// Función pura (no usa base de datos): por eso es fácil de probar con un test.
public static class PedidoValidador
{
    // Devuelve un diccionario { campo -> mensajes de error }. Vacío significa que todo es válido.
    public static Dictionary<string, string[]> Validar(CrearPedidoRequest request, bool skuExiste)
    {
        var errores = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(request.ClienteNombre))
            errores["clienteNombre"] = ["El nombre del cliente es obligatorio."];

        if (string.IsNullOrWhiteSpace(request.Sku))
            errores["sku"] = ["El SKU es obligatorio."];
        else if (!skuExiste)
            errores["sku"] = ["El SKU no existe en el catálogo."];

        if (request.Cantidad < 1 || request.Cantidad > 100)
            errores["cantidad"] = ["La cantidad debe estar entre 1 y 100."];

        return errores;
    }
}
