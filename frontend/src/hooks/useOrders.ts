import { useCallback, useEffect, useState } from "react";
import { getOrders } from "../api/ordersApi";
import type { Order } from "../types";

// Carga la lista de pedidos y la vuelve a pedir cada 3 segundos (polling).
export function useOrders(intervalMs = 3000) {
  const [orders, setOrders] = useState<Order[]>([]);
  const [error, setError] = useState<string | null>(null);

  const reload = useCallback(async () => {
    try {
      setOrders(await getOrders());
      setError(null);
    } catch (e) {
      setError(e instanceof Error ? e.message : "Error desconocido.");
    }
  }, []);

  useEffect(() => {
    reload();
    const id = setInterval(reload, intervalMs);
    return () => clearInterval(id); // limpia el temporizador al desmontar el componente
  }, [reload, intervalMs]);

  return { orders, error, reload };
}
