import { OrderForm } from "./components/OrderForm";
import { OrderList } from "./components/OrderList";
import { useOrders } from "./hooks/useOrders";

// El estado de la lista vive aquí (en el padre) para que el formulario pueda pedir un refresco al crear un pedido.
export default function App() {
  const { orders, error, reload } = useOrders();

  return (
    <main>
      <h1>OrderFlow</h1>
      <OrderForm onCreated={reload} />
      <OrderList orders={orders} error={error} />
    </main>
  );
}
