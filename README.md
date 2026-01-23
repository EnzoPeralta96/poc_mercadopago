# POC Mercado Pago - E-commerce con Clean Architecture

Prueba de concepto (POC) de integracion con Mercado Pago utilizando dos metodos de pago: **Checkout Pro** y **QR Dinamico**.

## Descripcion

Este proyecto es una aplicacion web de e-commerce que integra el sistema de pagos de Mercado Pago. Incluye:

- Catalogo de productos
- Carrito de compras con persistencia en sesion
- **Checkout Pro**: Redireccion al sitio de Mercado Pago
- **Pago con QR Dinamico**: Codigo QR que el cliente escanea con la app de MP
- Webhooks para actualizar el estado de las ordenes
- **SignalR**: Notificaciones en tiempo real cuando se completa el pago QR

## Tecnologias Utilizadas

| Componente | Tecnologia | Version |
|------------|------------|---------|
| Framework | ASP.NET Core | 10.0 |
| SDK de Pago | Mercado Pago .NET SDK | 2.11.0 |
| Tiempo Real | SignalR | Incluido en ASP.NET Core |
| Generacion QR | QRCoder | Ultima version |
| Almacenamiento | Archivos JSON | N/A |
| Carrito | Session Storage | N/A |
| Tunel desarrollo | ngrok | N/A |

## Arquitectura del Proyecto

```
poc_mercadopago/
├── Application/                    # Logica de negocio
│   ├── DTOs/                       # Data Transfer Objects
│   │   ├── StartCheckoutDTO/       # DTOs para Checkout Pro
│   │   └── StartQrDTO/             # DTOs para QR Dinamico
│   └── Services/
│       └── PaymentService/         # Servicio de pagos
│
├── Infrastructure/                 # Implementaciones de infraestructura
│   ├── Cart/                       # Almacenamiento del carrito
│   ├── Gateways/
│   │   └── MercadoPago/
│   │       ├── Configuration/      # MercadoPagoOptions, MercadoPagoQrOptions
│   │       ├── DTO/                # DTOs de comunicacion con MP
│   │       ├── MercadoPagoGateway/ # Gateway Checkout Pro
│   │       └── MercadoPagoQRGateway/ # Gateway QR Dinamico
│   ├── QRCode/                     # Generador de imagenes QR
│   ├── Session/                    # Extensiones de sesion
│   └── SignalR/                    # Notificaciones tiempo real
│       ├── Hub/                    # PaymentNotificationHub
│       └── NotificationService/    # Servicio de notificaciones
│
├── Models/                         # Modelos de dominio
│   ├── Order/                      # Order, OrderItem, OrderStatus
│   └── Product.cs
│
├── Presentation/                   # Capa de presentacion
│   ├── Controllers/                # HomeController, CartController, etc.
│   ├── Views/                      # Vistas Razor
│   └── WebHooks/                   # Controladores de webhooks
│
├── Repository/                     # Repositorios de datos
│   ├── OrderRepository/
│   └── ProductRepository/
│
├── Data/                           # Archivos JSON de datos
├── wwwroot/                        # Archivos estaticos (JS, CSS)
│   └── js/
│       ├── cart.js                 # Logica del carrito
│       └── qr-payment.js           # Cliente SignalR
│
└── doc/                            # Documentacion
    ├── integracion-checkoutPro.md  # Documentacion Checkout Pro
    ├── integracion-pagoQr.md       # Documentacion QR Dinamico
    ├── implementacion.md           # Documentacion tecnica completa
    └── guia-usuario.md             # Guia para usuarios no tecnicos
```

## Documentacion

| Documento | Descripcion |
|-----------|-------------|
| [integracion-checkoutPro.md](doc/integracion-checkoutPro.md) | Detalles de la integracion con Checkout Pro |
| [integracion-pagoQr.md](doc/integracion-pagoQr.md) | Detalles de la integracion con QR Dinamico |
| [implementacion.md](doc/implementacion.md) | Documentacion tecnica completa de toda la POC |
| [guia-usuario.md](doc/guia-usuario.md) | Guia de alto nivel para usuarios no tecnicos |

## Requisitos Previos

- [.NET 10.0 SDK](https://dotnet.microsoft.com/download)
- [ngrok](https://ngrok.com/download) (para recibir webhooks)
- Cuenta de Mercado Pago (modo prueba)
- Test Users de Mercado Pago (para QR Dinamico)

## Configuracion

### 1. Clonar el Repositorio

```bash
git clone https://github.com/EnzoPeralta96/poc_mercadopago.git
cd poc_mercadopago
```

### 2. Configurar Mercado Pago

Edita el archivo `appsettings.json`:

```json
{
  "MercadoPago": {
    "AccessToken": "TU_ACCESS_TOKEN",
    "PublicKey": "TU_PUBLIC_KEY",
    "BaseUrl": "TU_URL_DE_NGROK"
  },
  "MercadoPagoQr": {
    "AccessToken": "TOKEN_DEL_VENDEDOR_TEST",
    "PublicKey": "PUBLIC_KEY_DEL_VENDEDOR",
    "UserId": "ID_DEL_USUARIO_VENDEDOR",
    "ExternalPosId": "ID_DE_LA_CAJA",
    "BaseUrl": "TU_URL_DE_NGROK"
  }
}
```

**Nota:** Nunca subas el archivo `appsettings.json` con credenciales reales al repositorio.

### 3. Configuracion adicional para QR Dinamico

Para usar el pago con QR necesitas:

1. **Crear Test Users** en [Panel de Mercado Pago](https://www.mercadopago.com.ar/developers/panel/test-users)
   - Un usuario Vendedor
   - Un usuario Comprador

2. **Crear Sucursal y Caja** usando la API de MP:

```bash
# Crear sucursal
curl -X POST https://api.mercadopago.com/users/{user_id}/stores \
  -H "Authorization: Bearer {access_token}" \
  -d '{"name": "Mi Sucursal", "external_id": "SUC001"}'

# Crear caja
curl -X POST https://api.mercadopago.com/pos \
  -H "Authorization: Bearer {access_token}" \
  -d '{"name": "Caja 1", "external_store_id": "SUC001", "external_id": "SUC001POS001"}'
```

3. Usar el `external_id` de la caja como `ExternalPosId` en `appsettings.json`

### 4. Configurar ngrok

```bash
# Ejecutar ngrok
ngrok http https://localhost:5001
```

Copia la URL publica y actualiza `BaseUrl` en `appsettings.json`.

### 5. Restaurar Dependencias

```bash
dotnet restore
```

## Como Ejecutar

1. **Iniciar ngrok** (en una terminal separada):
```bash
ngrok http https://localhost:5001
```

2. **Actualizar BaseUrl** en `appsettings.json` con la URL de ngrok

3. **Ejecutar la aplicacion**:
```bash
dotnet run
```

4. **Abrir el navegador** en `https://localhost:5001`

## Metodos de Pago

### Checkout Pro

1. Usuario agrega productos al carrito
2. Click en "Pagar con Mercado Pago"
3. Se redirige a la pagina de Mercado Pago
4. Usuario completa el pago
5. MP redirige de vuelta a la aplicacion
6. Webhook actualiza el estado de la orden

### QR Dinamico

1. Usuario agrega productos al carrito
2. Click en "Pagar con QR"
3. Se genera un codigo QR unico
4. Usuario escanea con la app de Mercado Pago
5. Usuario confirma el pago en la app
6. SignalR notifica al navegador en tiempo real
7. La pagina se actualiza automaticamente

## Credenciales de Prueba

**Usuario de prueba:** `TESTUSER1564176571217246474`
**Contrasena:** `oIDPYGcGO1`

### Tarjetas de Prueba

**Aprobada:**
- Numero: `5031 7557 3453 0604`
- CVV: `123`
- Fecha: Cualquier fecha futura

**Rechazada:**
- Numero: `5031 4332 1540 6351`
- CVV: `123`
- Fecha: Cualquier fecha futura

Mas tarjetas: [Tarjetas de prueba Mercado Pago](https://www.mercadopago.com.ar/developers/es/docs/checkout-api/testing/test-cards)

## Funcionalidades

| Funcionalidad | Descripcion |
|---------------|-------------|
| Catalogo | Visualizacion de productos disponibles |
| Carrito | Agregar/eliminar productos, persistencia en sesion |
| Checkout Pro | Redireccion a Mercado Pago |
| Pago QR | Codigo QR dinamico por transaccion |
| Webhooks | Notificaciones de pago de MP |
| SignalR | Actualizacion en tiempo real |
| Ordenes | Creacion y seguimiento de ordenes |

## Estados de Ordenes

| Estado | Descripcion |
|--------|-------------|
| Created | Orden creada, sin procesar |
| Pending | Pago pendiente o en proceso |
| Approved | Pago aprobado |
| Rejected | Pago rechazado |

## Endpoints

### Aplicacion Web

| Metodo | Ruta | Descripcion |
|--------|------|-------------|
| GET | `/` | Catalogo de productos |
| POST | `/Checkout/Checkout` | Iniciar Checkout Pro |
| POST | `/MercadoPago/paymentQr` | Generar QR de pago |
| GET | `/checkout/return/{result}` | Resultado del pago |

### Webhooks

| Metodo | Ruta | Descripcion |
|--------|------|-------------|
| POST | `/webhooks/mercadopago` | Webhook Checkout Pro (type=payment) |
| POST | `/webhooks/mercadopago/qr` | Webhook QR (topic=merchant_order) |

### SignalR

| Hub | Ruta |
|-----|------|
| PaymentNotificationHub | `/hubs/payment-notification` |

## Solucion de Problemas

### El webhook no llega
1. Verifica que ngrok este corriendo
2. Confirma que la BaseUrl sea correcta
3. Revisa los logs de la aplicacion
4. Revisa el dashboard de ngrok en `http://127.0.0.1:4040`

### SignalR no notifica
1. Verifica en la consola del navegador (F12) que la conexion se establecio
2. Confirma que el cliente se unio al grupo de la orden
3. Revisa los logs del servidor

### Error 403 en QR
- El `UserId` no coincide con el dueno del `AccessToken`
- Ambos deben pertenecer al mismo Test User vendedor

### Error "sponsor y collector deben ser de tipos iguales"
- No incluir el campo `sponsor` cuando uses Test Users

## Notas de Desarrollo

- Este es un **POC**, no esta listo para produccion
- Los datos se almacenan en archivos JSON en `Data/`
- El carrito se almacena en sesion (se pierde al cerrar el navegador)
- En produccion, usar variables de entorno o Azure Key Vault para secrets
- Consultar la carpeta `doc/` para documentacion detallada

## Referencias

- [Mercado Pago - Checkout Pro](https://www.mercadopago.com.ar/developers/es/docs/checkout-pro/landing)
- [Mercado Pago - QR Dinamico](https://www.mercadopago.com.ar/developers/es/docs/qr-code/integration-configuration/qr-dynamic/integration)
- [ASP.NET Core SignalR](https://docs.microsoft.com/en-us/aspnet/core/signalr/introduction)
