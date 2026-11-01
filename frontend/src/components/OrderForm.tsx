import { useState, type FormEvent } from "react";
import { ApiError, crearPedido } from "../api/ordersApi";

type Errores = Record<string, string>;

interface Props {
  onCreated: () => void;
}

// Validación en el cliente: da respuesta inmediata, pero el servidor vuelve a validar siempre.
function validar(clienteNombre: string, sku: string, cantidad: string): Errores {
  const errores: Errores = {};
  if (!clienteNombre.trim()) errores.clienteNombre = "El nombre del cliente es obligatorio.";
  if (!sku.trim()) errores.sku = "El SKU es obligatorio.";
  const n = Number(cantidad);
  if (!Number.isInteger(n) || n < 1 || n > 100) errores.cantidad = "La cantidad debe estar entre 1 y 100.";
  return errores;
}

export function OrderForm({ onCreated }: Props) {
  const [clienteNombre, setClienteNombre] = useState("");
  const [sku, setSku] = useState("");
  const [cantidad, setCantidad] = useState("1");
  const [errores, setErrores] = useState<Errores>({});
  const [errorGeneral, setErrorGeneral] = useState<string | null>(null);
  const [enviando, setEnviando] = useState(false);

  async function handleSubmit(e: FormEvent) {
    e.preventDefault();
    setErrorGeneral(null);

    const erroresCliente = validar(clienteNombre, sku, cantidad);
    setErrores(erroresCliente);
    if (Object.keys(erroresCliente).length > 0) return;

    setEnviando(true);
    try {
      await crearPedido({ clienteNombre, sku, cantidad: Number(cantidad) });
      setClienteNombre("");
      setSku("");
      setCantidad("1");
      onCreated();
    } catch (err) {
      if (err instanceof ApiError) {
        // Errores 400 del servidor: se muestran bajo cada campo.
        const porCampo: Errores = {};
        for (const [campo, mensajes] of Object.entries(err.fieldErrors)) porCampo[campo] = mensajes[0];
        setErrores(porCampo);
        setErrorGeneral(err.message);
      } else {
        setErrorGeneral("Ocurrió un error inesperado.");
      }
    } finally {
      setEnviando(false);
    }
  }

  return (
    <form onSubmit={handleSubmit} noValidate>
      <h2>Nuevo pedido</h2>

      {errorGeneral && <p className="error" role="alert">{errorGeneral}</p>}

      <label>
        Cliente
        <input value={clienteNombre} onChange={(e) => setClienteNombre(e.target.value)} />
        {errores.clienteNombre && <span className="error">{errores.clienteNombre}</span>}
      </label>

      <label>
        SKU (prueba con ABC-01, ABC-02 o ABC-03)
        <input value={sku} onChange={(e) => setSku(e.target.value)} />
        {errores.sku && <span className="error">{errores.sku}</span>}
      </label>

      <label>
        Cantidad
        <input type="number" value={cantidad} onChange={(e) => setCantidad(e.target.value)} />
        {errores.cantidad && <span className="error">{errores.cantidad}</span>}
      </label>

      <button type="submit" disabled={enviando}>
        {enviando ? "Enviando..." : "Crear pedido"}
      </button>
    </form>
  );
}
