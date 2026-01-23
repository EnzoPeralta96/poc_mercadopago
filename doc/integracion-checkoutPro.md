# Documentación POC Mercado Pago - E-commerce

## Índice
1. [Descripción General](#descripción-general)
2. [Arquitectura del Proyecto](#arquitectura-del-proyecto)
3. [Estructura de Carpetas](#estructura-de-carpetas)
4. [Capas y Comunicación entre Módulos](#capas-y-comunicación-entre-módulos)
5. [Capa de Infrastructure - Integración Mercado Pago](#capa-de-infrastructure---integración-mercado-pago)
6. [Flujo de Pago Completo](#flujo-de-pago-completo)
7. [Modelos de Datos](#modelos-de-datos)
8. [Endpoints y Rutas](#endpoints-y-rutas)
9. [Configuración](#configuración)

---

## Descripción General

Esta POC (Proof of Concept) es una aplicación web de e-commerce que implementa la integración con **Mercado Pago Checkout Pro**. El proyecto demuestra:

- Catálogo de productos
- Carrito de compras con persistencia en sesión
- Procesamiento de pagos mediante Checkout Pro
- Manejo de webhooks para actualización de estado de órdenes
- Arquitectura limpia (Clean Architecture) adaptada para ASP.NET Core

### Tecnologías Utilizadas
- **Framework:** ASP.NET Core 10.0
- **SDK de Pago:** Mercado Pago .NET SDK v2.11.0
- **Almacenamiento:** JSON Files (para POC)
- **Estado de Carrito:** Session Storage
- **Túnel para desarrollo:** ngrok (webhooks)

---

## Arquitectura del Proyecto

El proyecto sigue una arquitectura inspirada en **Clean Architecture**, organizando el código en capas con responsabilidades claramente definidas:

```
┌─────────────────────────────────────────────────────────────┐
│                    PRESENTATION                             │
│  (Controllers, Views, ViewModels, WebHooks)                 │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                    APPLICATION                              │
│  (Services, DTOs - Lógica de negocio/orquestación)          │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                    INFRASTRUCTURE                           │
│  (Gateways externos, Cart Storage, Session, Configuration)  │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                    DOMAIN / MODELS                          │
│  (Entidades de negocio: Order, Product, OrderItem)          │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                    REPOSITORY                               │
│  (Persistencia: JsonProductRepository, JsonOrderRepository) │
└─────────────────────────────────────────────────────────────┘
```

### Principios Aplicados
- **Separación de responsabilidades:** Cada capa tiene una función específica
- **Inversión de dependencias:** Se usan interfaces (IPaymentService, IMercadoPagoGateway, etc.)
- **Inyección de dependencias:** Configurada en `Program.cs`

---

## Estructura de Carpetas

```
poc_mercadopago/
├── Application/                    # Capa de aplicación
│   ├── DTOs/                       # Data Transfer Objects
│   │   └── StartCheckoutDTO/
│   │       ├── StartCheckoutRequest.cs
│   │       ├── StartCheckoutRespone.cs
│   │       └── PaymentResultDTO.cs
│   └── Services/
│       └── PaymentService/         # Servicio de pagos
│           ├── IPaymentService.cs
│           └── PaymentService.cs
│
├── Infrastructure/                 # Capa de infraestructura
│   ├── Cart/                       # Almacenamiento del carrito
│   │   ├── CartStore/
│   │   │   ├── ICartStore.cs
│   │   │   └── SessionCartStore.cs
│   │   └── DTOs/
│   │       ├── CartItemDTO.cs
│   │       └── SessionCartDTO.cs
│   ├── Configuration/              # Configuración de MercadoPago
│   │   └── MercadoPagoOptions.cs   (nota: ubicado en Gateways/MercadoPago/Configuration)
│   ├── Gateways/
│   │   └── MercadoPago/            # Gateway de Mercado Pago
│   │       ├── Configuration/
│   │       │   └── MercadoPagoOptions.cs
│   │       ├── DTO/
│   │       │   ├── CreatePreferenceRequest.cs
│   │       │   ├── PreferenceItemDTO.cs
│   │       │   └── PaymentDetailsDTO.cs
│   │       └── MercadoPagoGateway/
│   │           ├── IMercadoPagoGateway.cs
│   │           └── MercadoPagoGateway.cs
│   └── Session/
│       └── SessionExtensions.cs    # Extensiones para serializar/deserializar en sesión
│
├── Models/                         # Modelos de dominio
│   ├── Order/
│   │   ├── Order.cs
│   │   ├── OrderItem.cs
│   │   └── OrderStatus.cs
│   └── Product.cs
│
├── Presentation/                   # Capa de presentación
│   ├── Controllers/
│   │   ├── HomeController.cs       # Catálogo de productos
│   │   ├── CartController.cs       # Gestión del carrito
│   │   └── CheckoutController.cs   # Proceso de checkout
│   ├── ViewModels/
│   │   ├── HomeViewModels/
│   │   ├── CartViewModels/
│   │   └── CheckoutViewModels/
│   ├── Views/                      # Vistas Razor
│   └── WebHooks/
│       └── MercadopagoWebhookController.cs  # Webhook de MP
│
├── Repository/                     # Repositorios de datos
│   ├── OrderRepository/
│   │   ├── IOrderRepository.cs
│   │   └── JsonOrderRepository.cs
│   └── ProductRepository/
│       ├── IProductRepository.cs
│       └── JsonProductRepository.cs
│
├── Data/                           # Archivos JSON de datos
│   ├── products.json
│   └── orders.json
│
├── Program.cs                      # Punto de entrada y configuración DI
└── appsettings.json                # Configuración de la aplicación
```

---

## Capas y Comunicación entre Módulos

### Diagrama de Comunicación

```
┌──────────────────────────────────────────────────────────────────────────┐
│                         USUARIO (Browser)                                │
└─────────────────────────────────┬────────────────────────────────────────┘
                                  │
                                  ▼
┌──────────────────────────────────────────────────────────────────────────┐
│                         PRESENTATION LAYER                               │
│  ┌─────────────────┐  ┌─────────────────┐   ┌─────────────────┐          │
│  │ HomeController  │  │ CartController  │   │CheckoutControlle│          │
│  │   (Catálogo)    │  │   (Carrito)     │   │   (Checkout)    │          │
│  └────────┬────────┘  └────────┬────────┘   └────────┬────────┘          │
│           │                    │                     │                   │
│  ┌────────┴────────────────────┴─────────────────────┘                   │
│  │                                                                       │
│  │  ┌─────────────────────────────────┐                                  │
│  │  │  MercadopagoWebhookController   │◄───── Webhook de Mercado Pago    │
│  │  └─────────────────────────────────┘                                  │
└──┼───────────────────────────────────────────────────────────────────────┘
   │
   ▼
┌──────────────────────────────────────────────────────────────────────────┐
│                         APPLICATION LAYER                                │
│  ┌─────────────────────────────────────────────────────────────────┐     │
│  │                        PaymentService                           │     │
│  │  - StartCheckoutAsync(): Crea orden + preferencia de MP         │     │
│  │  - GetPaymentResultAsync(): Consulta pago y actualiza orden     │     │
│  └─────────────────────────────────────────────────────────────────┘     │
└──────────────────────────────────────────────────────────────────────────┘
                                  │
          ┌───────────────────────┼───────────────────────┐
          ▼                       ▼                       ▼
┌──────────────────┐  ┌──────────────────┐  ┌──────────────────────────────┐
│   REPOSITORY     │  │  INFRASTRUCTURE  │  │     INFRASTRUCTURE           │
│  ┌────────────┐  │  │  ┌────────────┐  │  │  ┌────────────────────────┐  │
│  │ Product    │  │  │  │ CartStore  │  │  │  │  MercadoPagoGateway    │  │
│  │ Repository │  │  │  │ (Session)  │  │  │  │  - CreatePreference    │  │
│  └────────────┘  │  │  └────────────┘  │  │  │  - GetPayment          │  │
│  ┌────────────┐  │  │                  │  │  └────────────────────────┘  │
│  │ Order      │  │  │                  │  │             │                │
│  │ Repository │  │  │                  │  │             ▼                │
│  └────────────┘  │  │                  │  │    ┌──────────────────┐      │
└──────────────────┘  └──────────────────┘  │    │  MERCADO PAGO    │      │
          │                    │            │    │     API          │      │
          ▼                    ▼            │    └──────────────────┘      │
    ┌──────────┐         ┌──────────┐       └──────────────────────────────┘
    │ JSON     │         │ Session  │
    │ Files    │         │ Storage  │
    └──────────┘         └──────────┘
```

### Flujo de Dependencias

1. **Presentation → Application**: Los controladores inyectan `IPaymentService`
2. **Presentation → Infrastructure**: Los controladores inyectan `ICartStore`
3. **Presentation → Repository**: Los controladores inyectan `IProductRepository`
4. **Application → Infrastructure**: `PaymentService` inyecta `IMercadoPagoGateway`
5. **Application → Repository**: `PaymentService` inyecta `IOrderRepository` e `IProductRepository`

### Registro de Dependencias (Program.cs)

```csharp
// Carrito en Session
builder.Services.AddScoped<ICartStore, SessionCartStore>();

// Repositorios
builder.Services.AddSingleton<IProductRepository, JsonProductRepository>();
builder.Services.AddScoped<IOrderRepository, JsonOrderRepository>();

// Servicios de aplicación
builder.Services.AddScoped<IPaymentService, PaymentService>();

// Configuración de Mercado Pago
builder.Services.Configure<MercadoPagoOptions>(
    builder.Configuration.GetSection(MercadoPagoOptions.SectionName)
);

// Gateway de Mercado Pago
builder.Services.AddScoped<IMercadoPagoGateway, MercadoPagoGateway>();
```

---

## Capa de Infrastructure - Integración Mercado Pago

### Descripción General

La integración con Mercado Pago se encuentra en `Infrastructure/Gateways/MercadoPago/` y está compuesta por:

### 1. Configuración (`MercadoPagoOptions.cs`)

Define las opciones de configuración necesarias para conectar con Mercado Pago:

```csharp
public sealed class MercadoPagoOptions
{
    public const string SectionName = "MercadoPago";

    [Required]
    [MinLength(20)]
    public string AccessToken { get; set; }  // Token de acceso privado

    [Required]
    [MinLength(20)]
    public string PublicKey { get; set; }    // Clave pública (frontend)

    [Required]
    [Url]
    public string BaseUrl { get; set; }      // URL base para callbacks (ngrok)
}
```

**Características:**
- Validación con Data Annotations
- Validación al inicio de la aplicación (`ValidateOnStart`)
- Se lee desde la sección `MercadoPago` del `appsettings.json`

### 2. Interface del Gateway (`IMercadoPagoGateway.cs`)

```csharp
public interface IMercadoPagoGateway
{
    // Crea una preferencia de pago en Mercado Pago
    Task<string> CreatePreferenceAsync(CreatePreferenceRequest request, CancellationToken ct);

    // Obtiene los detalles de un pago por su ID
    Task<PaymentDetailsDTO> GetPaymentAsync(long paymentId, CancellationToken ct);
}
```

### 3. Implementación del Gateway (`MercadoPagoGateway.cs`)

#### Constructor y Configuración

```csharp
public MercadoPagoGateway(ILogger<MercadoPagoGateway> logger, IOptions<MercadoPagoOptions> options)
{
    _logger = logger;
    _mercadoPagoOptions = options.Value;
    MercadoPagoConfig.AccessToken = _mercadoPagoOptions.AccessToken;  // Configura el SDK
}
```

#### Método: `CreatePreferenceAsync`

Crea una preferencia de pago que permite al usuario pagar con Checkout Pro:

```csharp
public async Task<string> CreatePreferenceAsync(CreatePreferenceRequest request, CancellationToken ct)
{
    var preferenceRequest = new PreferenceRequest
    {
        // Referencia externa = ID de la orden interna (para correlacionar)
        ExternalReference = request.OrderId,

        // Items a pagar
        Items = request.Items.Select(item => new PreferenceItemRequest
        {
            Title = item.Title,
            Quantity = item.Quantity,
            UnitPrice = item.UnitPrice,
            CurrencyId = item.CurrencyId
        }).ToList(),

        // URLs de retorno después del pago
        BackUrls = new PreferenceBackUrlsRequest
        {
            Success = $"{_mercadoPagoOptions.BaseUrl}/checkout/return/success",
            Failure = $"{_mercadoPagoOptions.BaseUrl}/checkout/return/failure",
            Pending = $"{_mercadoPagoOptions.BaseUrl}/checkout/return/pending",
        },

        // Redirección automática si el pago es aprobado
        AutoReturn = "approved",

        // URL del webhook para notificaciones
        NotificationUrl = $"{_mercadoPagoOptions.BaseUrl}/webhooks/mercadopago"
    };

    var client = new PreferenceClient();
    var preference = await client.CreateAsync(preferenceRequest);

    return preference.Id;  // Retorna el ID de la preferencia
}
```

**Elementos clave:**
- `ExternalReference`: Vincula la preferencia con el ID de orden interno
- `BackUrls`: URLs a las que MP redirige después del pago
- `NotificationUrl`: URL del webhook donde MP envía notificaciones
- `AutoReturn`: Redirige automáticamente cuando el pago es aprobado

#### Método: `GetPaymentAsync`

Consulta los detalles de un pago específico:

```csharp
public async Task<PaymentDetailsDTO> GetPaymentAsync(long paymentId, CancellationToken ct)
{
    var client = new PaymentClient();
    var payment = await client.GetAsync(paymentId, cancellationToken: ct);

    return new PaymentDetailsDTO
    {
        PaymentId = payment.Id.Value,
        Status = payment.Status,              // approved, rejected, pending, etc.
        OrderId = payment.ExternalReference,  // ID de la orden interna
        Amount = payment.TransactionAmount ?? 0,
        CurrencyId = payment.CurrencyId
    };
}
```

### 4. DTOs del Gateway

#### `CreatePreferenceRequest.cs`
```csharp
public sealed record CreatePreferenceRequest
{
    public string OrderId { get; init; } = string.Empty;
    public List<PreferenceItemDTO> Items { get; init; } = [];
}
```

#### `PreferenceItemDTO.cs`
```csharp
public sealed record PreferenceItemDTO
{
    public string Title { get; init; } = string.Empty;
    public int Quantity { get; init; }
    public decimal UnitPrice { get; init; }
    public string CurrencyId { get; init; } = "ARS";
}
```

#### `PaymentDetailsDTO.cs`
```csharp
public sealed record PaymentDetailsDTO
{
    public long PaymentId { get; init; }
    public string Status { get; init; }      // approved, rejected, pending
    public string OrderId { get; init; }     // ExternalReference
    public decimal Amount { get; init; }
    public string CurrencyId { get; init; }
}
```

### 5. Almacenamiento del Carrito (Cart)

#### Interface `ICartStore.cs`
```csharp
public interface ICartStore
{
    Task<SessionCartDTO> GetCartAsync();      // Obtiene el carrito actual
    Task SaveCartAsync(SessionCartDTO cart);  // Guarda el carrito
    Task ClearCartAsync();                    // Limpia el carrito
}
```

#### Implementación `SessionCartStore.cs`

Almacena el carrito en la sesión del usuario:

```csharp
public class SessionCartStore : ICartStore
{
    private const string CartKey = "CART_KEY";
    private readonly IHttpContextAccessor _httpContextAccessor;

    public Task<SessionCartDTO> GetCartAsync()
    {
        var cart = Session.GetObjectFromJson<SessionCartDTO>(CartKey)
                   ?? new SessionCartDTO();
        return Task.FromResult(cart);
    }

    public Task SaveCartAsync(SessionCartDTO cart)
    {
        Session.SetObjectAsJson(CartKey, cart);
        return Task.CompletedTask;
    }

    public Task ClearCartAsync()
    {
        Session.Remove(CartKey);
        return Task.CompletedTask;
    }
}
```

#### Extensions de Sesión (`SessionExtensions.cs`)

```csharp
public static class SessionExtensions
{
    public static void SetObjectAsJson<T>(this ISession session, string key, T value)
       => session.SetString(key, JsonSerializer.Serialize(value));

    public static T GetObjectFromJson<T>(this ISession session, string key)
    {
        var json = session.GetString(key);
        return json == null ? default : JsonSerializer.Deserialize<T>(json);
    }
}
```

---

## Flujo de Pago Completo

### Diagrama de Secuencia

```
┌────────┐     ┌────────────────┐      ┌──────────────┐     ┌─────────────────┐     ┌────────────┐
│Usuario │     │CheckoutCtrl    │      │PaymentService│     │MercadoPagoGW    │     │MercadoPago │
└───┬────┘     └───────┬────────┘      └──────┬───────┘     └───────┬─────────┘     └─────┬──────┘
    │                  │                      │                     │                     │
    │ Click "Pagar"    │                      │                     │                     │
    │─────────────────>│                      │                     │                     │
    │                  │                      │                     │                     │
    │                  │ StartCheckoutAsync() │                     │                     │
    │                  │─────────────────────>│                     │                     │
    │                  │                      │                     │                     │
    │                  │                      │ Crear Orden         │                     │
    │                  │                      │──────────┐          │                     │
    │                  │                      │          │          │                     │
    │                  │                      │<─────────┘          │                     │
    │                  │                      │                     │                     │
    │                  │                      │ CreatePreferenceAsync()                   │
    │                  │                      │────────────────────>│                     │
    │                  │                      │                     │                     │
    │                  │                      │                     │  Create Preference  │
    │                  │                      │                     │────────────────────>│
    │                  │                      │                     │                     │
    │                  │                      │                     │   PreferenceId      │
    │                  │                      │                     │<────────────────────│
    │                  │                      │                     │                     │
    │                  │                      │   PreferenceId      │                     │
    │                  │                      │<────────────────────│                     │
    │                  │                      │                     │                     │
    │                  │ {PreferenceId, PK}   │                     │                     │
    │                  │<─────────────────────│                     │                     │
    │                  │                      │                     │                     │
    │  _CheckoutWallet │                      │                     │                     │
    │  (con MP Button) │                      │                     │                     │
    │<─────────────────│                      │                     │                     │
    │                  │                      │                     │                     │
    │──────────────────────────────────────────────────────────────────────────────────>  │
    │                                    Usuario paga en Mercado Pago                     │
    │<──────────────────────────────────────────────────────────────────────────────────  │
    │                                                                                     │
    │                  │                      │                     │    Webhook POST     │
    │                  │                      │                     │<─────────────────── │
    │                  │                      │                     │                     │
```

### Descripción Paso a Paso

#### 1. Usuario agrega productos al carrito
- `CartController.AddToCart()` agrega items al carrito en sesión
- El carrito se persiste con `ICartStore.SaveCartAsync()`

#### 2. Usuario inicia el checkout
- `CheckoutController.Checkout()` es invocado
- Obtiene los items del carrito vía `ICartStore`
- Llama a `PaymentService.StartCheckoutAsync()`

#### 3. PaymentService crea la orden
```csharp
// Valida que los productos existan
var productsSelected = await _productRepository.GetByIdsAsync(productsIds);

// Crea la orden con estado Pending
var order = CreateOrder(request.Items, productsSelected);
await _orderRepository.AddAsync(order);

// Crea la preferencia en Mercado Pago
var preferenceId = await _gateway.CreatePreferenceAsync(preferenceRequest);

// Actualiza la orden con el ID de preferencia
order.MercadoPagoPreferenceId = preferenceId;
await _orderRepository.UpdateAsync(order);
```

#### 4. Frontend muestra el botón de Mercado Pago
- Se renderiza `_CheckoutWallet.cshtml` con `PreferenceId` y `PublicKey`
- El SDK de MP en el frontend usa estos datos para mostrar el botón de pago

#### 5. Usuario completa el pago en Mercado Pago
- Es redirigido a la página de MP
- Paga con tarjeta, dinero en cuenta, etc.
- MP redirige al usuario a la BackUrl correspondiente

#### 6. Webhook recibe la notificación
```csharp
[HttpPost("webhooks/mercadopago")]
public async Task<IActionResult> Receive([FromQuery] string? type, [FromQuery] long? id)
{
    if (type != "payment" || !paymentId.HasValue)
        return Ok();

    // Obtiene detalles del pago y actualiza la orden
    var paymentResult = await _paymentService.GetPaymentResultAsync(paymentId.Value);
    return Ok();
}
```

#### 7. PaymentService actualiza la orden
```csharp
public async Task<PaymentResultDTO> GetPaymentResultAsync(long paymentId)
{
    // Obtiene detalles del pago desde MP
    var paymentDetails = await _gateway.GetPaymentAsync(paymentId);

    // Busca la orden por el ExternalReference
    var order = await _orderRepository.GetByIdAsync(paymentDetails.OrderId);

    // Actualiza el estado de la orden
    order.Status = MapPaymentStatusToOrderStatus(paymentDetails.Status);
    order.MercadoPagoPaymentId = paymentDetails.PaymentId;

    await _orderRepository.UpdateAsync(order);
}
```

### Mapeo de Estados

```csharp
private OrderStatus MapPaymentStatusToOrderStatus(string? mercadoPagoStatus)
{
    return mercadoPagoStatus?.ToLower() switch
    {
        "approved" => OrderStatus.approved,
        "pending" => OrderStatus.Pending,
        "in_process" => OrderStatus.Pending,
        "rejected" => OrderStatus.Rejected,
        "cancelled" => OrderStatus.Rejected,
        "refunded" => OrderStatus.Rejected,
        "charged_back" => OrderStatus.Rejected,
        _ => OrderStatus.Pending
    };
}
```

---

## Modelos de Datos

### Order (Orden)

```csharp
public class Order
{
    public string Id { get; init; }                      // GUID sin guiones
    public string Title { get; init; }                   // "Orden {Id}"
    public decimal Total { get; init; }                  // Total calculado
    public string CurrencyId { get; init; } = "ARS";
    public OrderStatus Status { get; set; }              // Created, Pending, Approved, Rejected
    public DateTimeOffset CreatedAt { get; init; }
    public List<OrderItem> Items { get; init; } = [];
    public string? MercadoPagoPreferenceId { get; set; } // ID de preferencia de MP
    public long? MercadoPagoPaymentId { get; set; }      // ID de pago de MP
}
```

### OrderItem

```csharp
public class OrderItem
{
    public string ProductId { get; init; }
    public string Title { get; init; }
    public int Quantity { get; init; }
    public decimal UnitPrice { get; init; }
    public string CurrencyId { get; init; } = "ARS";
    public decimal SubTotal => Quantity * UnitPrice;     // Calculado
}
```

### OrderStatus

```csharp
public enum OrderStatus
{
    Created,   // Orden creada pero aún no enviada a MP
    Pending,   // Pago pendiente
    approved,  // Pago aprobado
    Rejected   // Pago rechazado
}
```

### Product

```csharp
public class Product
{
    public string Id { get; init; }           // P001, P002, etc.
    public string Name { get; init; }
    public string Description { get; init; }
    public decimal Price { get; init; }
    public string CurrencyId { get; init; } = "ARS";
}
```

### Ejemplo de Orden en JSON

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
    },
    {
      "ProductId": "P008",
      "Title": "Cinta Métrica 5m",
      "Quantity": 2,
      "UnitPrice": 4200,
      "CurrencyId": "ARS"
    }
  ],
  "MercadoPagoPreferenceId": "3068452461-xyz...",
  "MercadoPagoPaymentId": 1234567890
}
```

---

## Endpoints y Rutas

### Controladores MVC

| Método | Ruta | Controlador | Descripción |
|--------|------|-------------|-------------|
| GET | `/` | HomeController.Index | Muestra catálogo de productos |
| GET | `/Cart/GetCartPartial` | CartController | Obtiene HTML del carrito |
| POST | `/Cart/AddToCart` | CartController | Agrega producto al carrito |
| POST | `/Cart/UpdateCart` | CartController | Actualiza cantidad |
| POST | `/Cart/RemoveFromCart` | CartController | Elimina producto del carrito |
| POST | `/Cart/ClearCart` | CartController | Vacía el carrito |
| POST | `/Checkout/Checkout` | CheckoutController | Inicia proceso de pago |
| GET | `/checkout/return/{result}` | CheckoutController | Página de resultado del pago |

### Webhook

| Método | Ruta | Controlador | Descripción |
|--------|------|-------------|-------------|
| POST | `/webhooks/mercadopago` | MercadopagoWebhookController | Recibe notificaciones de MP |

### Parámetros del Webhook

```
POST /webhooks/mercadopago?type=payment&id=123456789
POST /webhooks/mercadopago?type=payment&data.id=123456789
```

El webhook acepta el ID del pago tanto en `id` como en `data.id`.

---

## Configuración

### appsettings.json

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "MercadoPago": {
    "AccessToken": "APP_USR-...",
    "PublicKey": "APP_USR-...",
    "BaseUrl": "https://xxxxx.ngrok-free.dev"
  },
  "AllowedHosts": "*"
}
```

### Variables de Configuración

| Variable | Descripción | Ejemplo |
|----------|-------------|---------|
| `AccessToken` | Token de acceso privado de MP | `APP_USR-1979678...` |
| `PublicKey` | Clave pública para el frontend | `APP_USR-1f30dec5...` |
| `BaseUrl` | URL pública (ngrok) para callbacks | `https://xxx.ngrok-free.dev` |

### Configuración de Sesión

```csharp
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});
```

---

## Notas Importantes

### Seguridad (Pendiente de Implementar)

El webhook actualmente no valida que las peticiones provengan de Mercado Pago. En producción se debe:

1. Verificar la firma del webhook
2. Validar que el origen sea de MP
3. Implementar rate limiting
4. Sanitizar los parámetros de entrada

### Limitaciones de la POC

1. **Almacenamiento en JSON**: No apto para producción, usar base de datos real
2. **Sesión en memoria**: El carrito se pierde al reiniciar la app
3. **Sin autenticación de usuarios**: Cualquiera puede hacer compras
4. **Sin validación de webhook**: Potencial vulnerabilidad de seguridad
5. **Credenciales en appsettings**: Usar secretos/Key Vault en producción

### Productos de Prueba

El archivo `Data/products.json` contiene 20 productos de ferretería de ejemplo con precios en ARS.

---

## Resumen de Archivos Principales

| Archivo | Responsabilidad |
|---------|-----------------|
| `Program.cs` | Configuración DI, middleware, rutas |
| `MercadoPagoGateway.cs` | Comunicación con API de Mercado Pago |
| `PaymentService.cs` | Orquestación del proceso de pago |
| `CheckoutController.cs` | Endpoint de checkout y retorno |
| `MercadopagoWebhookController.cs` | Recepción de notificaciones de MP |
| `SessionCartStore.cs` | Persistencia del carrito en sesión |
| `JsonOrderRepository.cs` | Persistencia de órdenes en JSON |
