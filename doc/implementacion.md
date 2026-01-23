# Documentacion Tecnica Completa - POC Mercado Pago

## Indice General

1. [Vision General del Proyecto](#1-vision-general-del-proyecto)
2. [Arquitectura del Sistema](#2-arquitectura-del-sistema)
3. [Estructura de Carpetas Completa](#3-estructura-de-carpetas-completa)
4. [Capas de la Aplicacion](#4-capas-de-la-aplicacion)
5. [Integracion con Mercado Pago](#5-integracion-con-mercado-pago)
6. [Flujos de Pago Implementados](#6-flujos-de-pago-implementados)
7. [Sistema de Notificaciones en Tiempo Real](#7-sistema-de-notificaciones-en-tiempo-real)
8. [Modelos de Datos](#8-modelos-de-datos)
9. [Configuracion y Despliegue](#9-configuracion-y-despliegue)
10. [Seguridad y Consideraciones](#10-seguridad-y-consideraciones)

---

## 1. Vision General del Proyecto

### 1.1 Descripcion

Esta Prueba de Concepto (POC) es una aplicacion web de e-commerce que demuestra la integracion completa con **Mercado Pago** utilizando dos metodos de pago diferentes:

1. **Checkout Pro**: Redireccion al sitio de Mercado Pago para completar el pago
2. **QR Dinamico**: Generacion de codigo QR que el cliente escanea con la app de Mercado Pago

### 1.2 Funcionalidades Implementadas

| Funcionalidad         | Descripcion                              |
|-----------------------|------------------------------------------|
| Catalogo de productos | Listado de productos desde archivo JSON  |
| Carrito de compras    | Persistencia en sesion del navegador     |
| Checkout Pro          | Redireccion a Mercado Pago               |
| Pago con QR           | Codigo QR dinamico por transaccion       |
| Webhooks              | Recepcion de notificaciones de pago      |
| SignalR               | Notificaciones en tiempo real al cliente |
| Ordenes               | Creacion y actualizacion de ordenes      |

### 1.3 Stack Tecnologico

| Componente | Tecnologia            | Version |
|------------|------------ ----------|---------|
| Framework  | ASP.NETCore           | 10.0 |
| SDK de Pago| Mercado Pago .NET SDK | 2.11.0 |
| Comunicacion tiempo real | SignalR | Incluido en ASP.NET Core |
| Generacion QR | QRCoder            | Ultima version |
| Almacenamiento | Archivos JSON     | N/A |
| Carrito | Session Storage          | N/A |
| Tunel desarrollo | ngrok           | N/A |

---

## 2. Arquitectura del Sistema

### 2.1 Patron Arquitectonico

El proyecto implementa una arquitectura basada en **Clean Architecture** adaptada para ASP.NET Core, organizando el codigo en capas con responsabilidades claramente definidas.

### 2.2 Diagrama de Capas

```
+------------------------------------------------------------------+
|                     PRESENTATION LAYER                            |
|  Controllers, Views, ViewModels, WebHooks, JavaScript             |
+------------------------------------------------------------------+
                              |
                              v
+------------------------------------------------------------------+
|                     APPLICATION LAYER                             |
|  Services, DTOs - Logica de negocio y orquestacion                |
+------------------------------------------------------------------+
                              |
                              v
+------------------------------------------------------------------+
|                    INFRASTRUCTURE LAYER                           |
|  Gateways externos, Cart Storage, SignalR, QR Generator           |
+------------------------------------------------------------------+
                              |
                              v
+------------------------------------------------------------------+
|                     DOMAIN / MODELS                               |
|  Entidades de negocio: Order, Product, OrderItem, OrderStatus     |
+------------------------------------------------------------------+
                              |
                              v
+------------------------------------------------------------------+
|                      REPOSITORY LAYER                             |
|  Persistencia: JsonProductRepository, JsonOrderRepository         |
+------------------------------------------------------------------+
```

### 2.3 Diagrama de Comunicacion Completo

```
+----------------+
|    Usuario     |
|   (Browser)    |
+-------+--------+
        |
        v
+-------+--------+     +-----------------------+
|  Controllers   |<----|  WebHooks Controller  |<---- Mercado Pago
|  - Home        |     |  - CheckoutPro        |      (Notificaciones)
|  - Cart        |     |  - QR Dinamico        |
|  - Checkout    |     +-----------------------+
|  - MercadoPago |
+-------+--------+
        |
        v
+-------+-------------------------------+
| PaymentService                        |
|  - StartCheckoutAsync()               |
|  - StartQrAsync()                     |
|  - GetPaymentResultAsync()            |
|  - ProcessMerchantOrderWebhookAsync() |
+-------+-------------------------------+
        |
   +----+----+--------------------+
   |         |                    |
   v         v                    v
+--+---+ +---+----+    +----------+----------+
|Order | |Product |    | MercadoPago Gateways|
|Repo  | |Repo    |    | - CheckoutPro       |
+--+---+ +---+----+    | - QR Dinamico       |
   |         |         +----------+----------+
   v         v                    |
+--+---+ +---+----+               v
| JSON | | JSON   |        +-----+------+
|orders| |products|        | Mercado    |
+------+ +--------+        | Pago API   |
                           +------------+
```

### 2.4 Principios SOLID Aplicados

| Principio | Aplicacion en el Proyecto |
|-----------|---------------------------|
| Single Responsibility | Cada clase tiene una unica responsabilidad |
| Open/Closed | Interfaces permiten extender sin modificar |
| Liskov Substitution | Implementaciones son intercambiables |
| Interface Segregation | Interfaces especificas por funcionalidad |
| Dependency Inversion | Dependencia de abstracciones (interfaces) |

---

## 3. Estructura de Carpetas Completa

```
poc_mercadopago/
|
+-- Application/                         # Capa de aplicacion
|   +-- DTOs/                            # Data Transfer Objects
|   |   +-- StartCheckoutDTO/            # DTOs para Checkout Pro
|   |   |   +-- StartCheckoutRequest.cs
|   |   |   +-- StartCheckoutResponse.cs
|   |   |   +-- PaymentResultDTO.cs
|   |   +-- StartQrDTO/                  # DTOs para QR Dinamico
|   |       +-- StartQrRequest.cs
|   |       +-- StartQrResponse.cs
|   |       +-- QrPaymentStatusDTO.cs
|   +-- Services/
|       +-- PaymentService/              # Servicio principal de pagos
|           +-- IPaymentService.cs       # Interfaz
|           +-- PaymentService.cs        # Implementacion
|
+-- Infrastructure/                      # Capa de infraestructura
|   +-- Cart/                            # Manejo del carrito
|   |   +-- CartStore/
|   |   |   +-- ICartStore.cs
|   |   |   +-- SessionCartStore.cs
|   |   +-- DTOs/
|   |       +-- CartItemDTO.cs
|   |       +-- SessionCartDTO.cs
|   +-- Gateways/
|   |   +-- MercadoPago/
|   |       +-- Configuration/
|   |       |   +-- MercadoPagoOptions.cs      # Config Checkout Pro
|   |       |   +-- MercadoPagoQrOptions.cs    # Config QR
|   |       +-- DTO/
|   |       |   +-- CreatePreferenceRequest.cs
|   |       |   +-- PreferenceItemDTO.cs
|   |       |   +-- PaymentDetailsDTO.cs
|   |       |   +-- QrDTO/
|   |       |       +-- QrOrderRequest.cs
|   |       |       +-- QrOrderItemRequest.cs
|   |       |       +-- QrOrderResponse.cs
|   |       |       +-- MerchantOrderStatusResponse.cs
|   |       +-- MercadoPagoGateway/           # Gateway Checkout Pro
|   |       |   +-- IMercadoPagoGateway.cs
|   |       |   +-- MercadoPagoGateway.cs
|   |       +-- MercadoPagoQRGateway/         # Gateway QR
|   |           +-- IMercadoPagoQRGateway.cs
|   |           +-- MercadoPagoQRGateway.cs
|   +-- QRCode/                          # Generacion de imagenes QR
|   |   +-- IQrCodeGenerator.cs
|   |   +-- QrCodeGenerator.cs
|   +-- Session/
|   |   +-- SessionExtensions.cs
|   +-- SignalR/                         # Comunicacion tiempo real
|       +-- DTO/
|       |   +-- PaymentCompletedNotification.cs
|       +-- Hub/
|       |   +-- PaymentNotificationHub.cs
|       +-- NotificationService/
|           +-- IPaymentNotificationService.cs
|           +-- PaymentNotificacionService.cs
|
+-- Models/                              # Modelos de dominio
|   +-- Order/
|   |   +-- Order.cs
|   |   +-- OrderItem.cs
|   |   +-- OrderStatus.cs
|   +-- Product.cs
|
+-- Presentation/                        # Capa de presentacion
|   +-- Controllers/
|   |   +-- HomeController.cs            # Catalogo
|   |   +-- CartController.cs            # Carrito
|   |   +-- CheckoutController.cs        # Checkout y retornos
|   |   +-- MercadoPagoController.cs     # Endpoint QR
|   +-- ViewModels/
|   |   +-- HomeViewModels/
|   |   +-- CartViewModels/
|   |   +-- CheckoutViewModels/
|   |   +-- QrViewModels/
|   |       +-- QrViewModel.cs
|   +-- Views/
|   |   +-- Home/
|   |   +-- Cart/
|   |   |   +-- _Qr.cshtml               # Vista del QR
|   |   +-- Checkout/
|   |   +-- Shared/
|   +-- WebHooks/
|       +-- MercadopagoWebhookController.cs
|
+-- Repository/                          # Repositorios
|   +-- OrderRepository/
|   |   +-- IOrderRepository.cs
|   |   +-- JsonOrderRepository.cs
|   +-- ProductRepository/
|       +-- IProductRepository.cs
|       +-- JsonProductRepository.cs
|
+-- Data/                                # Archivos de datos
|   +-- products.json
|   +-- orders.json
|
+-- wwwroot/                             # Archivos estaticos
|   +-- js/
|   |   +-- cart.js                      # Logica del carrito
|   |   +-- qr-payment.js                # Cliente SignalR
|   +-- css/
|   +-- lib/
|
+-- doc/                                 # Documentacion
|   +-- integracion-checkoutPro.md
|   +-- integracion-pagoQr.md
|   +-- implementacion.md
|   +-- guia-usuario.md
|
+-- Program.cs                           # Punto de entrada
+-- appsettings.json                     # Configuracion
+-- appsettings.Development.json
```

---

## 4. Capas de la Aplicacion

### 4.1 Capa de Presentacion (Presentation)

#### Controllers

| Controlador | Responsabilidad | Rutas Principales |
|-------------|-----------------|-------------------|
| HomeController | Catalogo de productos | GET / |
| CartController | Gestion del carrito | POST /Cart/AddToCart, GET /Cart/GetCartPartial |
| CheckoutController | Proceso de checkout | POST /Checkout/Checkout, GET /checkout/return/{result} |
| MercadoPagoController | Pago con QR | POST /MercadoPago/paymentQr |
| MercadopagoWebhookController | Webhooks de MP | POST /webhooks/mercadopago, POST /webhooks/mercadopago/qr |

#### Vistas Principales

| Vista | Descripcion |
|-------|-------------|
| Home/Index.cshtml | Catalogo de productos |
| Cart/_Cart.cshtml | Contenido del carrito |
| Cart/_Qr.cshtml   | Codigo QR para pago |
| Checkout/_CheckoutWallet.cshtml | Boton de Mercado Pago |
| Checkout/Return.cshtml | Resultado del pago |

#### JavaScript

| Archivo       | Funciones Principales|
|---------------|----------------------|
| cart.js       | loadCartContent(), addToCart(), proceedToQr(), cancelQrCheckout() |
| qr-payment.js | QrPaymentNotificationClient, startQrPaymentNotificationClient() |

### 4.2 Capa de Aplicacion (Application)

#### PaymentService

Este es el servicio central que orquesta toda la logica de pago:

```csharp
public interface IPaymentService
{
    // Checkout Pro
    Task<StartCheckoutResponse> StartCheckoutAsync(StartCheckoutRequest request, CancellationToken ct);
    Task<PaymentResultDTO> GetPaymentResultAsync(long paymentId, CancellationToken ct);

    // QR Dinamico
    Task<StartQrResponse> StartQrAsync(StartQrRequest request, CancellationToken ct);
    Task<QrPaymentStatusDTO> ProcessMerchantOrderWebhookAsync(long merchantOrderId, CancellationToken ct);
}
```

#### Flujo interno de StartCheckoutAsync:

1. Validar que los productos existen
2. Crear orden local con estado Created
3. Crear preferencia en Mercado Pago
4. Actualizar orden con PreferenceId
5. Retornar datos para mostrar boton de pago

#### Flujo interno de StartQrAsync:

1. Validar que los productos existen
2. Crear orden local con estado Created
3. Crear orden QR en Mercado Pago
4. Generar imagen QR desde el qr_data
5. Actualizar orden con InStoreOrderId
6. Retornar datos para mostrar QR

### 4.3 Capa de Infraestructura (Infrastructure)

#### Gateways de Mercado Pago

**MercadoPagoGateway (Checkout Pro)**
- Usa el SDK oficial de Mercado Pago
- Crea preferencias de pago
- Consulta estado de pagos

**MercadoPagoQRGateway (QR Dinamico)**
- Usa HttpClient directo (no hay SDK para QR)
- Endpoint: POST /instore/orders/qr/seller/collectors/{user_id}/pos/{pos_id}/qrs
- Consulta merchant orders

#### SignalR Components

| Componente | Funcion   |
|------------|-----------|
| PaymentNotificationHub | Hub que gestiona grupos por orderId |
| PaymentNotificationService | Envia notificaciones a grupos |
| PaymentCompletedNotification | DTO de la notificacion |

#### QR Code Generator

Usa la libreria QRCoder para convertir el string EMV de Mercado Pago en una imagen PNG codificada en Base64.

### 4.4 Capa de Dominio (Models)

#### Order

```csharp
public class Order
{
    public string Id { get; init; }                      // GUID
    public string Title { get; init; }
    public decimal Total { get; init; }
    public string CurrencyId { get; init; } = "ARS";
    public OrderStatus Status { get; set; }
    public DateTimeOffset CreatedAt { get; init; }
    public List<OrderItem> Items { get; init; } = [];

    // Checkout Pro
    public string? MercadoPagoPreferenceId { get; set; }
    public long? MercadoPagoPaymentId { get; set; }

    // QR Dinamico
    public string? MercadoPagoInStoreOrderId { get; set; }
}
```

#### OrderStatus

```csharp
public enum OrderStatus
{
    Created,    // Orden creada, sin procesar
    Pending,    // Pago en proceso
    approved,   // Pago aprobado
    Rejected    // Pago rechazado
}
```

### 4.5 Capa de Repositorio (Repository)

| Repositorio | Almacenamiento | Operaciones |
|-------------|----------------|-------------|
| JsonProductRepository | Data/products.json | GetAll, GetById, GetByIds |
| JsonOrderRepository | Data/orders.json | Add, Update, GetById, GetAll |

---

## 5. Integracion con Mercado Pago

### 5.1 Checkout Pro

#### Configuracion

```json
{
  "MercadoPago": {
    "AccessToken": "APP_USR-xxxxx",
    "PublicKey": "APP_USR-xxxxx",
    "BaseUrl": "https://xxx.ngrok.dev"
  }
}
```

#### Flujo de Datos

```
Usuario -> CheckoutController -> PaymentService -> MercadoPagoGateway -> API MP
                                                                            |
                                                                            v
Usuario <- Vista con boton MP <- Controller <- Service <- PreferenceId <- API MP
```

#### Webhook de Payment

- Endpoint: POST /webhooks/mercadopago
- Parametros: type=payment, id={payment_id}
- Accion: Consulta /v1/payments/{id} y actualiza orden

### 5.2 QR Dinamico

#### Configuracion

```json
{
  "MercadoPagoQr": {
    "AccessToken": "APP_USR-xxxxx",
    "PublicKey": "APP_USR-xxxxx",
    "UserId": "3068452461",
    "ExternalPosId": "SUC001POS001",
    "BaseUrl": "https://xxx.ngrok.dev"
  }
}
```

#### Prerrequisitos en Mercado Pago

1. **Sucursal (Store)**: Representa la tienda fisica
2. **Caja (POS)**: Punto de venta asociado a la sucursal
3. **Test Users**: Vendedor y Comprador para ambiente sandbox

#### Flujo de Datos

```
Usuario -> MercadoPagoController -> PaymentService -> MercadoPagoQRGateway -> API MP
                                                                                  |
                                                                                  v
Usuario <- Vista con QR <- Controller <- Service <- qr_data + in_store_order_id <- API MP
```

#### Webhook de Merchant Order

- Endpoint: POST /webhooks/mercadopago/qr
- Parametros: topic=merchant_order, id={merchant_order_id}
- Accion: Consulta /merchant_orders/{id}, extrae external_reference, actualiza orden

### 5.3 Comparativa de Integraciones

| Aspecto | Checkout Pro | QR Dinamico |
|---------|--------------|-------------|
| SDK | Mercado Pago .NET SDK | HttpClient directo |
| Endpoint creacion | PreferenceClient.CreateAsync() | POST /instore/orders/qr/... |
| Tipo de notificacion | payment | merchant_order |
| Estado pagado | approved | closed |
| Consulta de estado | GET /v1/payments/{id} | GET /merchant_orders/{id} |
| external_reference | En el Payment | En la MerchantOrder |

---

## 6. Flujos de Pago Implementados

### 6.1 Flujo Checkout Pro

```
+--------+     +------------+     +---------------+     +--------------+     +------------+
|Usuario |     |Checkout    |     |Payment        |     |MercadoPago   |     |MercadoPago |
|        |     |Controller  |     |Service        |     |Gateway       |     |API         |
+---+----+     +-----+------+     +-------+-------+     +------+-------+     +-----+------+
    |                |                    |                    |                   |
    | Click Pagar    |                    |                    |                   |
    |--------------->|                    |                    |                   |
    |                | StartCheckoutAsync |                    |                   |
    |                |------------------->|                    |                   |
    |                |                    | Crear Orden        |                   |
    |                |                    |-------+            |                   |
    |                |                    |       |            |                   |
    |                |                    |<------+            |                   |
    |                |                    |                    |                   |
    |                |                    | CreatePreferenceAsync                  |
    |                |                    |------------------->|                   |
    |                |                    |                    | Create Preference |
    |                |                    |                    |------------------>|
    |                |                    |                    |                   |
    |                |                    |                    |   PreferenceId    |
    |                |                    |                    |<------------------|
    |                |                    |                    |                   |
    |                |                    |<-------------------|                   |
    |                |                    |                    |                   |
    |                |<-------------------|                    |                   |
    |                |                    |                    |                   |
    | Boton MP       |                    |                    |                   |
    |<---------------|                    |                    |                   |
    |                |                    |                    |                   |
    |--------------- Redirige a MP, usuario paga, MP redirige a BackUrl ---------->|
    |                |                    |                    |                   |
    |                |                    |                    |   Webhook POST    |
    |                |                    |                    |<------------------|
    |                |                    |                    |                   |
```

### 6.2 Flujo QR Dinamico

```
+--------+     +------------+     +---------------+     +--------------+     +------------+
|Usuario |     |MercadoPago |     |Payment        |     |MercadoPago   |     |MercadoPago |
|        |     |Controller  |     |Service        |     |QRGateway     |     |API         |
+---+----+     +-----+------+     +-------+-------+     +------+-------+     +-----+------+
    |                |                    |                    |                   |
    | Pagar con QR   |                    |                    |                   |
    |--------------->|                    |                    |                   |
    |                | StartQrAsync       |                    |                   |
    |                |------------------->|                    |                   |
    |                |                    | Crear Orden        |                   |
    |                |                    |-------+            |                   |
    |                |                    |<------+            |                   |
    |                |                    |                    |                   |
    |                |                    | CreateQrOrderAsync |                   |
    |                |                    |------------------->|                   |
    |                |                    |                    | POST /instore/... |
    |                |                    |                    |------------------>|
    |                |                    |                    |                   |
    |                |                    |                    |  qr_data + id     |
    |                |                    |                    |<------------------|
    |                |                    |                    |                   |
    |                |                    | Generar imagen QR  |                   |
    |                |                    |-------+            |                   |
    |                |                    |<------+            |                   |
    |                |                    |                    |                   |
    |                |<-------------------|                    |                   |
    |                |                    |                    |                   |
    | Imagen QR      |                    |                    |                   |
    |<---------------|                    |                    |                   |
    |                |                    |                    |                   |
    | Escanea QR con app MP              |                    |                   |
    | Paga en la app                     |                    |                   |
    |                |                    |                    |                   |
    |                |                    |                    |   Webhook POST    |
    |                |                    |                    |<------------------|
    |                |                    |                    |                   |
```

### 6.3 Flujo de Notificacion SignalR

```
+------------+     +------------+     +---------------+     +------------+     +--------+
|MercadoPago |     |Webhook     |     |Payment        |     |SignalR     |     |Browser |
|API         |     |Controller  |     |Service        |     |Service     |     |        |
+-----+------+     +-----+------+     +-------+-------+     +-----+------+     +---+----+
      |                  |                    |                   |               |
      | POST webhook     |                    |                   |               |
      |----------------->|                    |                   |               |
      |                  | ProcessWebhookAsync|                   |               |
      |                  |------------------->|                   |               |
      |                  |                    | GetMerchantOrder  |               |
      |                  |                    |--------+          |               |
      |                  |                    |        |          |               |
      |<-----------------+--------------------+--------+          |               |
      |  GET /merchant_orders/{id}                                |               |
      |---------------------------------------------------------->|               |
      |  {status, external_reference, ...}                        |               |
      |<----------------------------------------------------------|               |
      |                  |                    |                   |               |
      |                  |                    | Actualizar orden  |               |
      |                  |                    |-------+           |               |
      |                  |                    |<------+           |               |
      |                  |                    |                   |               |
      |                  |<-------------------|                   |               |
      |                  |                    |                   |               |
      |                  | NotifyPaymentCompletedAsync            |               |
      |                  |--------------------------------------->|               |
      |                  |                    |                   |               |
      |                  |                    |                   | PaymentCompleted
      |                  |                    |                   |-------------->|
      |                  |                    |                   |               |
      |                  |                    |                   |               | Redirige
      |                  |                    |                   |               |---->
```

---

## 7. Sistema de Notificaciones en Tiempo Real

### 7.1 Arquitectura SignalR

SignalR permite comunicacion bidireccional entre el servidor y los clientes web. En esta POC se usa para notificar al usuario cuando su pago QR ha sido procesado.

### 7.2 Componentes

#### PaymentNotificationHub

```csharp
public class PaymentNotificationHub : Hub<IPaymentNotificationClient>
{
    // Cliente se une al grupo de su orden
    public async Task JoinOrderGroup(string orderId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"order_{orderId}");
    }
}

public interface IPaymentNotificationClient
{
    Task PaymentCompleted(PaymentCompletedNotification notification);
}
```

#### PaymentNotificationService

```csharp
public class PaymentNotificationService : IPaymentNotificationService
{
    public async Task NotifyPaymentCompletdedAsync(string orderId, string status, long? paymentId)
    {
        var notification = new PaymentCompletedNotification
        {
            OrderId = orderId,
            Status = status,
            PaymentId = paymentId,
            Message = GenerateMessage(status)
        };

        await _hubContext.Clients.Group($"order_{orderId}").PaymentCompleted(notification);
    }
}
```

#### Cliente JavaScript

```javascript
class QrPaymentNotificationClient {
    async start(orderId, onPaymentCompleted, onError) {
        this.connection = new signalR.HubConnectionBuilder()
            .withUrl("/hubs/payment-notification")
            .withAutomaticReconnect()
            .build();

        this.connection.on("PaymentCompleted", (notification) => {
            onPaymentCompleted(notification);
        });

        await this.connection.start();
        await this.connection.invoke("JoinOrderGroup", orderId);
    }
}
```

### 7.3 Flujo de Grupos

1. Usuario solicita pago QR -> Se genera orderId
2. Vista QR se carga -> JavaScript inicia conexion SignalR
3. Cliente llama JoinOrderGroup(orderId) -> Se une al grupo "order_{orderId}"
4. Webhook llega -> Servidor procesa pago
5. Servidor llama NotifyPaymentCompletedAsync(orderId, status) -> SignalR envia a grupo
6. Cliente recibe notificacion -> Redirige segun resultado

---

## 8. Modelos de Datos

### 8.1 Order (Orden de Compra)

| Campo | Tipo | Descripcion |
|-------|------|-------------|
| Id | string | GUID unico de la orden |
| Title | string | Titulo descriptivo |
| Total | decimal | Monto total |
| CurrencyId | string | Moneda (default: ARS) |
| Status | OrderStatus | Estado actual |
| CreatedAt | DateTimeOffset | Fecha de creacion |
| Items | List<OrderItem> | Items de la orden |
| MercadoPagoPreferenceId | string? | ID preferencia (Checkout Pro) |
| MercadoPagoPaymentId | long? | ID pago MP |
| MercadoPagoInStoreOrderId | string? | ID orden QR |

### 8.2 OrderItem

| Campo | Tipo | Descripcion |
|-------|------|-------------|
| ProductId | string | ID del producto |
| Title | string | Nombre del producto |
| Quantity | int | Cantidad |
| UnitPrice | decimal | Precio unitario |
| CurrencyId | string | Moneda |
| SubTotal | decimal | Calculado: Quantity * UnitPrice |

### 8.3 Product

| Campo | Tipo | Descripcion |
|-------|------|-------------|
| Id | string | ID unico (P001, P002...) |
| Name | string | Nombre del producto |
| Description | string | Descripcion |
| Price | decimal | Precio |
| CurrencyId | string | Moneda |

### 8.4 Ejemplo JSON de Orden

```json
{
  "Id": "abc123def456",
  "Title": "Orden abc123def456",
  "Total": 15000,
  "CurrencyId": "ARS",
  "Status": 2,
  "CreatedAt": "2026-01-15T10:30:00-03:00",
  "Items": [
    {
      "ProductId": "P001",
      "Title": "Martillo de Acero",
      "Quantity": 1,
      "UnitPrice": 8500,
      "CurrencyId": "ARS"
    }
  ],
  "MercadoPagoPreferenceId": "3068452461-xyz",
  "MercadoPagoPaymentId": 1234567890,
  "MercadoPagoInStoreOrderId": null
}
```

---

## 9. Configuracion y Despliegue

### 9.1 appsettings.json Completo

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "MercadoPago": {
    "AccessToken": "APP_USR-xxxx",
    "PublicKey": "APP_USR-xxxx",
    "BaseUrl": "https://xxx.ngrok.dev"
  },
  "MercadoPagoQr": {
    "AccessToken": "APP_USR-xxxx",
    "PublicKey": "APP_USR-xxxx",
    "UserId": "3068452461",
    "ExternalPosId": "SUC001POS001",
    "BaseUrl": "https://xxx.ngrok.dev"
  },
  "AllowedHosts": "*"
}
```

### 9.2 Program.cs - Inyeccion de Dependencias

```csharp
// Session
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Carrito
builder.Services.AddScoped<ICartStore, SessionCartStore>();

// Repositorios
builder.Services.AddSingleton<IProductRepository, JsonProductRepository>();
builder.Services.AddScoped<IOrderRepository, JsonOrderRepository>();

// Servicio de pagos
builder.Services.AddScoped<IPaymentService, PaymentService>();

// Mercado Pago - Checkout Pro
builder.Services.Configure<MercadoPagoOptions>(
    builder.Configuration.GetSection(MercadoPagoOptions.SectionName));
builder.Services.AddScoped<IMercadoPagoGateway, MercadoPagoGateway>();

// Mercado Pago - QR
builder.Services.Configure<MercadoPagoQrOptions>(
    builder.Configuration.GetSection(MercadoPagoQrOptions.SectionName));
builder.Services.AddScoped<IMercadoPagoQRGateway, MercadoPagoQRGateway>();

// QR Code Generator
builder.Services.AddSingleton<IQrCodeGenerator, QrCodeGenerator>();

// SignalR
builder.Services.AddSignalR();
builder.Services.AddScoped<IPaymentNotificationService, PaymentNotificationService>();

// Mapeo del Hub
app.MapHub<PaymentNotificationHub>("/hubs/payment-notification");
```

### 9.3 Configuracion de ngrok

Para desarrollo local, ngrok expone el servidor local a internet para recibir webhooks:

```bash
# Iniciar ngrok
ngrok http 5000

# Obtener URL publica (ejemplo)
# https://abc123.ngrok.dev

# Actualizar BaseUrl en appsettings.json con esta URL
```

### 9.4 Configuracion de Mercado Pago

#### Test Users

1. Crear Test Users en: https://www.mercadopago.com.ar/developers/panel/test-users
2. Crear usuario Vendedor (seller)
3. Crear usuario Comprador (buyer)
4. Usar credenciales del Vendedor en appsettings.json

#### Sucursal y Caja (solo para QR)

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

---

## 10. Seguridad y Consideraciones

### 10.1 Limitaciones de la POC

| Limitacion | Descripcion | Solucion Produccion |
|------------|-------------|---------------------|
| Almacenamiento JSON | No escalable | Base de datos SQL/NoSQL |
| Session en memoria | Se pierde al reiniciar | Redis/SQL Server session |
| Sin autenticacion | Cualquiera puede comprar | Identity/OAuth |
| Sin validacion webhook | Vulnerable a ataques | Verificar firma MP |
| Credenciales en config | Expuestas en repositorio | Azure Key Vault/Secrets |

### 10.2 Validacion de Webhooks (Pendiente)

En produccion, se debe validar que las peticiones provienen de Mercado Pago:

1. Verificar firma del webhook usando la clave secreta
2. Validar que el origen sea de MP
3. Implementar rate limiting
4. Usar HTTPS obligatorio

### 10.3 Manejo de Errores

| Escenario | Manejo Actual |
|-----------|---------------|
| Producto no encontrado | Retorna error 400 |
| Orden no encontrada | Retorna null, log warning |
| Error API MP | Propaga excepcion |
| Webhook invalido | Retorna 200 (evita reintentos) |
| SignalR desconectado | Reconexion automatica |

### 10.4 Logging

El proyecto usa ILogger para registrar eventos importantes:

- Webhooks recibidos
- Ordenes creadas/actualizadas
- Errores de API
- Conexiones SignalR

---

## Resumen de Archivos Clave

| Archivo | Responsabilidad |
|---------|-----------------|
| Program.cs | Configuracion DI, middleware, rutas |
| PaymentService.cs | Orquestacion de todo el proceso de pago |
| MercadoPagoGateway.cs | Comunicacion con API MP (Checkout Pro) |
| MercadoPagoQRGateway.cs | Comunicacion con API MP (QR) |
| QrCodeGenerator.cs | Genera imagenes QR desde string EMV |
| PaymentNotificationHub.cs | Hub SignalR para grupos |
| PaymentNotificacionService.cs | Envia notificaciones SignalR |
| MercadopagoWebhookController.cs | Recibe webhooks de MP |
| qr-payment.js | Cliente SignalR en el navegador |
| cart.js | Logica del carrito y UI |

---

## Referencias

- [Mercado Pago - Checkout Pro](https://www.mercadopago.com.ar/developers/es/docs/checkout-pro/landing)
- [Mercado Pago - QR Dinamico](https://www.mercadopago.com.ar/developers/es/docs/qr-code/integration-configuration/qr-dynamic/integration)
- [ASP.NET Core SignalR](https://docs.microsoft.com/en-us/aspnet/core/signalr/introduction)
- [Clean Architecture](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html)
