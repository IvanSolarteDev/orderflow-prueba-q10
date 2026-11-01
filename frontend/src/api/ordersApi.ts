import type { CrearPedidoRequest, Order } from "../types";

const API_URL = import.meta.env.VITE_API_URL;

// Error con el detalle por campo que devuelve el servidor cuando responde 400.
export class ApiError extends Error {
  fieldErrors: Record<string, string[]>;

  constructor(message: string, fieldErrors: Record<string, string[]> = {}) {
    super(message);
    this.fieldErrors = fieldErrors;
  }
}

// fetch lanza TypeError si no hay red o el servidor está caído; lo convertimos en un mensaje claro.
async function request(path: string, init?: RequestInit): Promise<Response> {
  try {
    return await fetch(`${API_URL}${path}`, init);
  } catch {
    throw new ApiError("No se pudo conectar con el servidor.");
  }
}

export async function getOrders(): Promise<Order[]> {
  const res = await request("/orders");
  if (!res.ok) throw new ApiError(`Error del servidor (${res.status}).`);
  return res.json();
}

export async function crearPedido(pedido: CrearPedidoRequest): Promise<Order> {
  const res = await request("/orders", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(pedido),
  });

  if (res.status === 400) {
    const body = await res.json();
    throw new ApiError("Hay datos inválidos en el formulario.", body.errors);
  }
  if (!res.ok) throw new ApiError(`Error del servidor (${res.status}).`);
  return res.json();
}
