export type EstadoPedido = "Pending" | "Confirmed" | "Rejected";

export interface Order {
  id: string;
  clienteNombre: string;
  sku: string;
  cantidad: number;
  estado: EstadoPedido;
  creadoEn: string;
}

export interface CrearPedidoRequest {
  clienteNombre: string;
  sku: string;
  cantidad: number;
}
