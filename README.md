# OrderFlow

Panel web para registrar pedidos de una tienda en línea y ver su estado. Cada pedido reserva inventario antes de confirmarse: si hay stock se **confirma**, si no se **rechaza**.

Dos servicios .NET se comunican de forma asíncrona por RabbitMQ, y una app React muestra el estado en vivo.

## Cómo correrlo (menos de 10 minutos)

**Requisitos:** Docker con Docker Compose. Nada más.

```bash
docker compose up --build
```

La primera vez tarda unos minutos porque descarga imágenes y compila. Cuando termine:

| Qué | URL |
|---|---|
| **Panel web** | http://localhost:3000 |
| API de pedidos | http://localhost:8080/orders |
| Panel de RabbitMQ | http://localhost:15672 (usuario `orderflow`, contraseña `orderflow`) |

No hay pasos manuales: las bases de datos y las tablas se crean al arrancar, y el stock y el catálogo se cargan solos (seed).

**Datos de prueba (seed):**

| SKU | Stock disponible |
|---|---|
| `ABC-01` | 100 |
| `ABC-02` | 50 |
| `ABC-03` | 5 |

**Prueba rápida:** en el panel crea un pedido `ABC-01` x2 y verás `Pending` → `Confirmed`. Crea uno `ABC-03` x10 y verás `Rejected` (solo hay 5).

Para borrar todos los datos y empezar de cero: `docker compose down -v`.

## Cómo correr los tests

Requiere el SDK de .NET 10.

```bash
dotnet test OrderFlow.slnx
```

Son 5 tests (9 casos) sobre la lógica crítica:

- **Validación:** nombre vacío, cantidad fuera de 1–100, SKU vacío y SKU que no existe en el catálogo.
- **Transiciones de estado:** `Pending → Confirmed`, y un pedido confirmado no puede rechazarse.
- **Idempotencia:** el mismo evento procesado dos veces descuenta el stock **una sola vez**.
- **Stock insuficiente:** rechaza y no descuenta.

## Arquitectura

```
 ┌──────────┐  POST/GET   ┌────────────┐  OrderCreated   ┌──────────────────┐
 │ Frontend │ ──────────► │ Orders.Api │ ──────────────► │ Inventory.Worker │
 │  React   │ ◄────────── │ (Postgres: │  (vía RabbitMQ) │ (Postgres:       │
 └──────────┘  polling 3s │  orders)   │ ◄────────────── │  inventory)      │
                          └────────────┘ StockReserved / └──────────────────┘
                                         StockRejected
```

| Carpeta | Qué contiene |
|---|---|
| `src/Contracts` | Los 3 eventos compartidos (`OrderCreated`, `StockReserved`, `StockRejected`) como `record` |
| `src/Orders.Api` | ASP.NET Core Minimal API: pedidos, catálogo, validación y consumidores de las respuestas |
| `src/Inventory.Worker` | Worker Service: stock, idempotencia y consumidor de `OrderCreated` |
| `tests/OrderFlow.Tests` | Tests con xUnit |
| `frontend` | React + Vite + TypeScript, sin librerías extra |

Los dos servicios **no se referencian entre sí**: solo comparten `Contracts`.

### Flujo de un pedido

1. El panel envía `POST /orders`. La API valida, guarda el pedido en `Pending` y guarda el evento `OrderCreated` en la misma transacción (Outbox).
2. Un proceso en segundo plano publica el evento en RabbitMQ.
3. `Inventory.Worker` lo recibe, descuenta el stock si alcanza y publica `StockReserved` o `StockRejected`.
4. `Orders.Api` recibe la respuesta y pasa el pedido a `Confirmed` o `Rejected`.
5. El panel consulta la lista cada 3 segundos y muestra el cambio.

### Endpoints

| Método | Ruta | Respuesta |
|---|---|---|
| `POST` | `/orders` | `201` con el pedido creado; `400` con el detalle por campo si los datos son inválidos |
| `GET` | `/orders` | Lista de pedidos con su estado actual |
| `GET` | `/orders/{id}` | Detalle; `404` si no existe |

## Decisiones técnicas y por qué

- **Una base de datos por servicio** (un solo contenedor de Postgres con las bases `orders` e `inventory`). Cada servicio es dueño de sus datos y no leen la base del otro.
- **MassTransit 8.x sobre RabbitMQ**, fijado en la versión 8 porque la 9 es comercial.
- **Transactional Outbox de MassTransit** en Orders: el evento se guarda en la misma transacción que el pedido, así nunca se crea un pedido sin su evento ni al revés.
- **Idempotencia en Inventory:** cada `EventId` procesado se guarda en la tabla `ProcessedEvents` **en la misma transacción** que el descuento. Si el evento llega otra vez, no se toca el stock.
- **Descuento atómico:** `UPDATE ... WHERE Disponible >= cantidad`. Dos pedidos simultáneos del mismo SKU no pueden dejar el stock negativo.
- **Idempotencia en Orders:** si el pedido ya no está en `Pending`, el evento se ignora. Las transiciones están dentro de la entidad (`Confirm()` y `Reject()`), que solo permite `Pending → Confirmed` o `Pending → Rejected`.
- **Lógica de reserva separada del consumidor** (`ReservaStock`): se puede probar con un test sin necesitar RabbitMQ.
- **Catálogo de productos en la base de Orders** para validar el SKU. Es la solución más simple. *Trade-off:* el catálogo de Orders y el stock de Inventory son datos duplicados que hay que mantener alineados (hoy los dos tienen el mismo seed).
- **Sin sobre-ingeniería:** sin repositorios genéricos, CQRS, MediatR ni AutoMapper. `DbContext` se usa directamente porque el problema es pequeño.
- **Configuración por variables de entorno.** Los servicios no arrancan si falta alguna (fallan con un mensaje claro). No hay cadenas de conexión en el código.
- **Dockerfiles multi-stage con imágenes alpine:** el SDK solo compila, y la imagen final lleva solo el runtime.
- **Frontend:** el estado de la lista vive en un hook (`useOrders`) con polling cada 3 s. La validación del cliente da respuesta inmediata, y los errores `400` del servidor se muestran bajo cada campo.

## Manejo de fallos

| Situación | Qué pasa |
|---|---|
| **Inventory no responde** | El mensaje espera en una cola **durable** de RabbitMQ y el pedido permanece en `Pending`. Cuando Inventory vuelve, lo procesa y el pedido pasa a `Confirmed` o `Rejected`. Hay 5 reintentos cada 2 segundos si el procesamiento falla. Verificado manualmente. |
| **El broker está caído al crear un pedido** | El pedido se crea igual (`201`) en `Pending`, porque el evento queda guardado en la base (Outbox). Cuando RabbitMQ vuelve, el evento se publica solo y el pedido avanza. Verificado manualmente. |
| **El mismo evento llega dos veces** | Inventory lo detecta por su `EventId` y no descuenta de nuevo. Si el procesamiento ya había terminado, reenvía el mismo resultado, por si la respuesta anterior se perdió. Orders ignora respuestas repetidas. Demostrado con test. |
| **La API no está disponible** | El panel muestra el error en pantalla y conserva los datos del formulario. |

## Trade-offs asumidos

- **Entrega al menos una vez**, no exactamente una: por eso los dos consumidores son idempotentes.
- **`EnsureCreated` en vez de migraciones de EF:** simplifica el arranque con un solo comando, pero no sirve para evolucionar el esquema en producción.
- **Polling cada 3 s** en vez de SignalR: más simple y suficiente para este caso.
- **CORS abierto** (`AllowAnyOrigin`): se justifica porque no hay autenticación, que el enunciado excluye.
- **Credenciales por defecto en `docker-compose.yml`** (`orderflow`/`orderflow`): son solo para desarrollo local y se pueden sobrescribir con variables de entorno (`POSTGRES_USER`, `POSTGRES_PASSWORD`, `RABBITMQ_USER`, `RABBITMQ_PASS`).
- **Los tests de Inventory usan SQLite en memoria** y no Postgres: son rápidos y sin dependencias, pero no ejercitan Postgres real. `dotnet test` muestra un aviso de vulnerabilidad en `SQLitePCLRaw`, que solo afecta al proyecto de tests.

## Qué haría distinto con más tiempo

- **Pedidos atascados:** hoy un pedido puede quedarse en `Pending` indefinidamente si Inventory nunca vuelve. Añadiría un tiempo máximo que lo pase a `Rejected` o lo marque para revisión.
- **Mensajes con error:** tras agotar los reintentos, MassTransit los envía a una cola `_error`. Faltan alertas y una forma de reprocesarlos.
- **Migraciones de EF Core** en lugar de `EnsureCreated`.
- **Concurrencia optimista** en la entidad `Order`, para proteger las transiciones ante respuestas simultáneas.
- **Limpieza periódica de `ProcessedEvents`**, que hoy crece sin límite.
- **Tests de integración de los endpoints** y del flujo completo con Testcontainers (Postgres y RabbitMQ reales).
- **Un endpoint `/health`** para liveness y readiness probes.
- **SignalR** para actualizar el panel en tiempo real.
- **Kubernetes:** no se incluyó (bonus opcional). Se haría un `Deployment`, `Service` y `ConfigMap` por servicio, con probes sobre `/health` y límites de recursos, y los secretos en `Secret` en lugar de variables en claro.
