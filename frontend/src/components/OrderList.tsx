import type { Order } from "../types";

interface Props {
  orders: Order[];
  error: string | null;
}

export function OrderList({ orders, error }: Props) {
  return (
    <section>
      <h2>Pedidos</h2>

      {error && <p className="error" role="alert">No se pudo actualizar la lista: {error}</p>}

      {orders.length === 0 ? (
        <p>Todavía no hay pedidos.</p>
      ) : (
        <table>
          <thead>
            <tr>
              <th>Cliente</th>
              <th>SKU</th>
              <th>Cantidad</th>
              <th>Estado</th>
              <th>Creado</th>
            </tr>
          </thead>
          <tbody>
            {orders.map((o) => (
              <tr key={o.id}>
                <td>{o.clienteNombre}</td>
                <td>{o.sku}</td>
                <td>{o.cantidad}</td>
                <td><span className={`estado ${o.estado}`}>{o.estado}</span></td>
                <td>{new Date(o.creadoEn).toLocaleString()}</td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </section>
  );
}
