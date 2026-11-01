using Orders.Api;

namespace OrderFlow.Tests;

public class PedidoTests
{
    // --- Validación ---

    [Theory]
    [InlineData("", "ABC-01", 1, true, "clienteNombre")]    // nombre vacío
    [InlineData("Ana", "ABC-01", 0, true, "cantidad")]      // cantidad menor a 1
    [InlineData("Ana", "ABC-01", 101, true, "cantidad")]    // cantidad mayor a 100
    [InlineData("Ana", "", 1, true, "sku")]                 // sku vacío
    [InlineData("Ana", "XXX", 1, false, "sku")]             // sku que no existe en el catálogo
    public void Validador_rechaza_datos_invalidos(string cliente, string sku, int cantidad, bool skuExiste, string campoConError)
    {
        var errores = PedidoValidador.Validar(new CrearPedidoRequest(cliente, sku, cantidad), skuExiste);

        Assert.Contains(campoConError, errores.Keys);
    }

    // --- Transiciones de estado ---

    [Fact]
    public void Pedido_nuevo_empieza_en_Pending_y_Confirm_lo_pasa_a_Confirmed()
    {
        var order = new Order("Ana", "ABC-01", 2);
        Assert.Equal(EstadoPedido.Pending, order.Estado);

        order.Confirm();

        Assert.Equal(EstadoPedido.Confirmed, order.Estado);
    }

    [Fact]
    public void Pedido_ya_confirmado_no_puede_rechazarse()
    {
        var order = new Order("Ana", "ABC-01", 2);
        order.Confirm();

        Assert.Throws<InvalidOperationException>(() => order.Reject());
        Assert.Equal(EstadoPedido.Confirmed, order.Estado);
    }
}
