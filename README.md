# POC Mercado Pago - E-commerce con Clean Architecture

Prueba de concepto (POC) de integración con Mercado Pago Checkout Pro.

## Descripción

Este proyecto es una aplicación web de e-commerce que integra el sistema de pagos de Mercado Pago. Incluye un carrito de compras, procesamiento de pagos mediante Checkout Pro, y manejo de webhooks para actualizar el estado de las órdenes.

## Tecnologías Utilizadas

- **Framework:** ASP.NET Core 10.0
- **SDK de Pago:** Mercado Pago .NET SDK
- **Almacenamiento:** JSON Files (para POC)
- **Estado:** Session Storage (carrito de compras)
- **Túnel:** ngrok (para webhooks en desarrollo)

## Arquitectura del Proyecto

```
poc_mercadopago/
├── Application/              # Lógica de negocio
│   ├── DTOs/                # Data Transfer Objects
│   └── Services/            # Servicios de aplicación
│       └── PaymentService/  # Servicio de pagos
├── Infrastructure/          # Implementaciones de infraestructura
│   ├── Cart/               # Almacenamiento del carrito
│   ├── Configuration/      # Configuraciones
│   └── Gateways/           # Integraciones externas
│       └── MercadoPago/    # Gateway de Mercado Pago
├── Models/                 # Modelos de dominio
│   ├── Order/             # Modelo de orden
│   └── Product/           # Modelo de producto
├── Presentation/          # Capa de presentación
│   ├── Controllers/       # Controladores MVC
│   ├── Views/            # Vistas Razor
│   └── WebHooks/         # Controladores de webhooks
└── Repository/           # Repositorios de datos
    ├── OrderRepository/  # Repositorio de órdenes
    └── ProductRepository/# Repositorio de productos
```

## Requisitos Previos

- [.NET 10.0 SDK](https://dotnet.microsoft.com/download)
- [ngrok](https://ngrok.com/download) (para recibir webhooks)
- Cuenta de Mercado Pago (modo prueba)

## Configuración

### 1. Clonar el Repositorio

```bash
git clone https://github.com/EnzoPeralta96/poc_mercadopago.git
cd poc_mercadopago
```

### 2. Configurar Mercado Pago

Edita el archivo `appsettings.json` y reemplaza con tus credenciales de Mercado Pago:

```json
{
  "MercadoPago": {
    "AccessToken": "TU_ACCESS_TOKEN",
    "PublicKey": "TU_PUBLIC_KEY",
    "BaseUrl": "TU_URL_DE_NGROK"
  }
}
```

**Nota:** Nunca subas el archivo `appsettings.json` con credenciales reales al repositorio. Está incluido aquí solo para fines de POC.

### 3. Configurar ngrok

ngrok es necesario para que Mercado Pago pueda enviar webhooks a tu aplicación local.

#### Instalación de ngrok

1. Descarga ngrok desde [https://ngrok.com/download](https://ngrok.com/download)
2. Extrae el ejecutable en una carpeta de tu preferencia
3. (Opcional) Crea una cuenta gratuita en ngrok para obtener un authtoken

#### Ejecutar ngrok

Primero, ejecuta tu aplicación .NET para saber en qué puerto está corriendo (generalmente 5000 o 5001).

```bash
dotnet run
```

En otra terminal, ejecuta ngrok:

```bash
ngrok http https://localhost:5001
```

O si tu aplicación corre en HTTP:

```bash
ngrok http http://localhost:5000
```

ngrok te dará una URL pública como:
```
Forwarding  https://xxxx-xx-xx-xxx-xxx.ngrok-free.app -> https://localhost:5001
```

#### Actualizar BaseUrl en appsettings.json

Copia la URL de ngrok (sin la barra al final) y actualiza el `appsettings.json`:

```json
{
  "MercadoPago": {
    "BaseUrl": "https://xxxx-xx-xx-xxx-xxx.ngrok-free.app"
  }
}
```

**Importante:** Cada vez que reinicies ngrok, la URL cambiará y deberás actualizarla en `appsettings.json`.

### 4. Restaurar Dependencias

```bash
dotnet restore
```

## Cómo Ejecutar

1. **Iniciar ngrok** (en una terminal separada):
```bash
ngrok http https://localhost:5001
```

2. **Actualizar BaseUrl** en `appsettings.json` con la URL de ngrok

3. **Ejecutar la aplicación**:
```bash
dotnet run
```

4. **Abrir el navegador** en `https://localhost:5001` (o el puerto que muestre la consola)

## Credenciales de Prueba de Mercado Pago

Para realizar pruebas de pago, utiliza la siguiente cuenta de prueba de Mercado Pago:

**Usuario:** `TESTUSER1564176571217246474`
**Contraseña:** `oIDPYGcGO1`

### Tarjetas de Prueba

Al momento de pagar, puedes usar estas tarjetas de prueba:

**Aprobada:**
- Número: `5031 7557 3453 0604`
- CVV: `123`
- Fecha: Cualquier fecha futura
- Titular: Cualquier nombre

**Rechazada:**
- Número: `5031 4332 1540 6351`
- CVV: `123`
- Fecha: Cualquier fecha futura
- Titular: Cualquier nombre

Más tarjetas de prueba en: [Tarjetas de prueba Mercado Pago](https://www.mercadopago.com.ar/developers/es/docs/checkout-api/testing/test-cards)

## Funcionalidades

### 1. Catálogo de Productos
- Visualización de productos disponibles
- Agregar productos al carrito

### 2. Carrito de Compras
- Agregar/eliminar productos
- Actualizar cantidades
- Persistencia en sesión
- Cálculo automático del total

### 3. Checkout con Mercado Pago
- Integración con Checkout Pro
- Creación de preferencia de pago
- Redirección a Mercado Pago
- Múltiples métodos de pago disponibles

### 4. Procesamiento de Pagos
- Webhooks de Mercado Pago
- Actualización automática del estado de la orden
- Asociación del payment ID de Mercado Pago con la orden interna

### 5. Estado de Órdenes
- Pending: Orden creada, pago pendiente
- Approved: Pago aprobado
- Rejected: Pago rechazado

## Flujo de Pago

1. El usuario agrega productos al carrito
2. El usuario hace clic en "Proceder al pago"
3. Se crea una orden en el sistema con estado "Pending"
4. Se genera una preferencia de pago en Mercado Pago
5. El usuario es redirigido a Mercado Pago para completar el pago
6. El usuario paga con su cuenta de prueba
7. Mercado Pago redirige al usuario de vuelta a la aplicación 
8. Mercado Pago envía un webhook con la notificación del pago
9. El sistema actualiza el estado de la orden y guarda el payment ID
10. El usuario ve el resultado del pago

## Estructura de Datos

### Order
```json
{
  "Id": "guid-interno",
  "Title": "Orden guid-interno",
  "Total": 6400,
  "CurrencyId": "ARS",
  "Status": 2,
  "CreatedAt": "2026-01-02T20:24:40.2566634-03:00",
  "Items": [...],
  "MercadoPagoPreferenceId": "3068452461-efb0955c-5830-41eb-ab07-a87392250e80",
  "MercadoPagoPaymentId": 1234567890
}
```

### Estados de Orden
- `1`: Pending (Pendiente)
- `2`: Approved (Aprobado)
- `3`: Rejected (Rechazado)

## Endpoints Importantes

### Aplicación Web
- `GET /` - Página principal con catálogo
- `POST /Checkout/Checkout` - Procesar checkout
- `GET /checkout/return/{result}` - Página de resultado del pago

### Webhooks
- `POST /webhooks/mercadopago` - Webhook de notificaciones de Mercado Pago

## Configuración de URLs en Mercado Pago

Las siguientes URLs se configuran automáticamente al crear la preferencia:

**Back URLs:**
- Success: `{BaseUrl}/checkout/return/success`
- Failure: `{BaseUrl}/checkout/return/failure`
- Pending: `{BaseUrl}/checkout/return/pending`

**Notification URL:**
- `{BaseUrl}/webhooks/mercadopago`

## Solución de Problemas

### El webhook no llega
1. Verifica que ngrok esté corriendo
2. Confirma que la BaseUrl en `appsettings.json` sea la correcta
3. Revisa los logs de la aplicación
4. Revisa el dashboard de ngrok en `http://127.0.0.1:4040`

### Error de redirección después del pago
- Verifica que las rutas del controlador coincidan con las BackUrls configuradas
- Revisa que el controlador `CheckoutController` tenga la ruta correcta

### La orden no se actualiza después del pago
- Verifica que el webhook esté llegando (logs de la aplicación)
- Confirma que el ExternalReference de Mercado Pago coincida con el ID de la orden

## Notas de Desarrollo

- Este es un **POC (Proof of Concept)**, no está listo para producción
- Los datos se almacenan en archivos JSON en la carpeta `Data/`
- El carrito se almacena en sesión (se pierde al cerrar el navegador)
- Las credenciales están en `appsettings.json` solo para fines de demostración
- En producción, usa variables de entorno o Azure Key Vault para secrets

