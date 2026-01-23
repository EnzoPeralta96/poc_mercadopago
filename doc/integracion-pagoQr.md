# Documentación Técnica: Integración de Pago con QR Dinámico (Mercado Pago Instore Orders)

**Versión:** 1.0
**Última actualización:** Enero 2026
**Arquitectura:** ASP.NET Core 10.0 con Arquitectura en Capas

---

## Tabla de Contenidos

1. [Resumen Ejecutivo](#1-resumen-ejecutivo)
2. [Prerrequisitos y Configuración](#2-prerrequisitos-y-configuración)
3. [Arquitectura del Módulo QR](#3-arquitectura-del-módulo-qr)
4. [Flujo de Generación de QR (El Request)](#4-flujo-de-generación-de-qr-el-request)
5. [Verificación del Pago (El Cierre)](#5-verificación-del-pago-el-cierre)
6. [Notificaciones en Tiempo Real (SignalR)](#6-notificaciones-en-tiempo-real-signalr)
7. [Guía de Uso Rápido](#7-guía-de-uso-rápido)
8. [Referencia de Código](#8-referencia-de-código)
9. [Troubleshooting](#9-troubleshooting)
10. [Lecciones Aprendidas](#10-lecciones-aprendidas)

---

## 1. Resumen Ejecutivo

Este módulo implementa **Cobros Presenciales con QR Dinámico** usando la API de Mercado Pago Instore Orders. Permite generar códigos QR únicos por transacción que el cliente escanea con la app de Mercado Pago para completar el pago.

### Características principales:
- Generación de QR dinámico por cada transacción
- Notificaciones en tiempo real vía SignalR cuando el pago se completa
- Redirección automática tras confirmación del pago
- Soporte para ambiente de desarrollo con Test Users

### Diferencia con Checkout Pro:
| Aspecto | Checkout Pro | QR Dinámico |
|---------|--------------|-------------|
| Flujo | Redirige a Mercado Pago | Usuario escanea QR en tu app |
| Preferencia | `PreferenceClient` | `POST /instore/orders/qr/...` |
| Notificación | Webhook `payment` | Webhook `merchant_order` |
| Validación | Consultar `/v1/payments/{id}` | Consultar `/merchant_orders/{id}` |

---

## 2. Prerrequisitos y Configuración

### 2.1 Requisitos Previos en Mercado Pago (No están en el código)

Antes de implementar QR dinámico, debes crear en tu cuenta de Mercado Pago:

#### a) Sucursal (Store)
```bash
# Crear sucursal vía API
POST https://api.mercadopago.com/users/{user_id}/stores
{
  "name": "Sucursal Principal",
  "location": {
    "street_name": "Av. Corrientes",
    "street_number": "1234",
    "city_name": "Buenos Aires"
  },
  "external_id": "SUC001"
}
```

#### b) Caja/Punto de Venta (POS)
```bash
# Crear caja asociada a la sucursal
POST https://api.mercadopago.com/pos
{
  "name": "Caja 1",
  "fixed_amount": false,
  "store_id": "{store_id}",
  "external_store_id": "SUC001",
  "external_id": "SUC001POS001"  # Este es el external_pos_id que usaremos
}
```

> **IMPORTANTE:** El `external_id` del POS (`SUC001POS001`) es el valor que configuraremos como `ExternalPosId` en `appsettings.json`. Este ID vincula las órdenes QR con la caja física.

### 2.2 Usuarios de Prueba (Test Users)

Para desarrollo/sandbox, Mercado Pago requiere **Test Users**:

1. **Vendedor (Seller):** Usuario que recibe el pago
2. **Comprador (Buyer):** Usuario que paga escaneando el QR

Crear test users en: https://www.mercadopago.com.ar/developers/panel/test-users

> **CRÍTICO:** El `AccessToken` en `appsettings.json` debe pertenecer al **mismo usuario** cuyo `UserId` se configura. Si el token es del usuario A pero el UserId es del usuario B, recibirás **Error 403 Forbidden**.

### 2.3 Configuración en appsettings.json

```json
{
  "MercadoPagoQr": {
    "AccessToken": "APP_USR-xxxx-xxxx",     // Token del vendedor (Test User)
    "PublicKey": "APP_USR-xxxx",            // Public Key del vendedor
    "UserId": "3068452461",                  // ID del usuario vendedor (debe coincidir con el token)
    "ExternalPosId": "SUC001POS001",        // external_id del POS creado
    "BaseUrl": "https://tu-dominio.ngrok.dev" // URL pública para webhooks
  }
}
```

### 2.4 Clase de Configuración

**Archivo:** `Infrastructure/Gateways/MercadoPago/Configuration/MercadoPagoQrOptions.cs`

```csharp
/// <summary>
/// Opciones de configuración para la integración de QR Dinámico con Mercado Pago.
/// Esta clase es SEPARADA de MercadoPagoOptions porque usa credenciales de una aplicación diferente.
/// </summary>
public sealed class MercadoPagoQrOptions
{
    public const string SectionName = "MercadoPagoQr";  // Nombre de la sección en appsettings.json

    [Required] public string AccessToken { get; set; }   // Token de acceso del vendedor
    [Required] public string PublicKey { get; set; }     // Clave pública (no usada en QR pero útil)
    [Required] public string UserId { get; set; }        // ID del usuario vendedor en MP
    [Required] public string ExternalPosId { get; set; } // ID externo del POS
    [Required] public string BaseUrl { get; set; }       // URL base para webhooks
}
```

### 2.5 Sobre el Sponsor (Modo Integrador)

> **IMPORTANTE para Sandbox:** En modo desarrollo con Test Users, **NO incluir** el campo `sponsor` en el request. El sponsor es para integradores certificados y mezclar usuarios de producción con test users genera el error:

```
"El user del sponsor y del collector deben ser de tipos iguales"
```

---

## 3. Arquitectura del Módulo QR

### 3.1 Diagrama de Capas

```
┌─────────────────────────────────────────────────────────────────┐
│                     PRESENTATION LAYER                          │
│  ┌─────────────────────┐  ┌──────────────────────────────────┐ │
│  │ MercadoPagoController│  │ MercadopagoWebhookController    │ │
│  │  - PaymentQr()      │  │  - ReceiveQrWebhook()           │ │
│  └─────────────────────┘  └──────────────────────────────────┘ │
│  ┌─────────────────────┐  ┌──────────────────────────────────┐ │
│  │ Views/Cart/_Qr.cshtml│  │ wwwroot/js/qr-payment.js        │ │
│  └─────────────────────┘  └──────────────────────────────────┘ │
└──────────────────────────────┬──────────────────────────────────┘
                               │
┌──────────────────────────────┴──────────────────────────────────┐
│                     APPLICATION LAYER                            │
│  ┌──────────────────────────────────────────────────────────┐   │
│  │ PaymentService                                            │   │
│  │  - StartQrAsync()                                         │   │
│  │  - ProcessMerchantOrderWebhookAsync()                     │   │
│  └──────────────────────────────────────────────────────────┘   │
│  ┌──────────────────────────────────────────────────────────┐   │
│  │ DTOs: StartQrRequest, StartQrResponse, QrPaymentStatusDTO │   │
│  └──────────────────────────────────────────────────────────┘   │
└──────────────────────────────┬──────────────────────────────────┘
                               │
┌──────────────────────────────┴──────────────────────────────────┐
│                    INFRASTRUCTURE LAYER                          │
│  ┌────────────────────────┐  ┌────────────────────────────────┐ │
│  │ MercadoPagoQRGateway   │  │ PaymentNotificationService     │ │
│  │  - CreateQrOrderAsync()│  │  - NotifyPaymentCompletedAsync │ │
│  │  - GetMerchantOrder... │  └────────────────────────────────┘ │
│  └────────────────────────┘  ┌────────────────────────────────┐ │
│  ┌────────────────────────┐  │ PaymentNotificationHub         │ │
│  │ QrCodeGenerator        │  │  - JoinOrderGroup()            │ │
│  │  - GenerateQrImage...  │  └────────────────────────────────┘ │
│  └────────────────────────┘                                     │
└─────────────────────────────────────────────────────────────────┘
```

### 3.2 Archivos del Módulo QR

| Archivo | Capa | Responsabilidad |
|---------|------|-----------------|
| `MercadoPagoController.cs` | Presentation | Endpoint `PaymentQr()` que inicia el flujo |
| `MercadopagoWebhookController.cs` | Presentation | Recibe webhooks de MP |
| `_Qr.cshtml` | Presentation | Vista que muestra el QR al usuario |
| `qr-payment.js` | Presentation | Cliente SignalR para notificaciones |
| `cart.js` | Presentation | Función `proceedToQr()` que carga el QR |
| `PaymentService.cs` | Application | Orquesta la creación de orden y QR |
| `IPaymentService.cs` | Application | Interfaz del servicio |
| `StartQrRequest/Response.cs` | Application | DTOs de entrada/salida |
| `QrPaymentStatusDTO.cs` | Application | DTO para estado del pago QR |
| `MercadoPagoQRGateway.cs` | Infrastructure | Comunicación con API de MP |
| `QrCodeGenerator.cs` | Infrastructure | Genera imagen QR desde string EMV |
| `PaymentNotificationHub.cs` | Infrastructure | Hub SignalR para grupos por orden |
| `PaymentNotificationService.cs` | Infrastructure | Envía notificaciones a clientes |
| `MercadoPagoQrOptions.cs` | Infrastructure | Configuración tipada |

---

## 4. Flujo de Generación de QR (El Request)

### 4.1 Diagrama de Secuencia

```
┌────────┐     ┌──────────────────┐     ┌───────────────┐     ┌──────────────────┐     ┌────────────┐
│Cliente │     │MercadoPagoController│  │PaymentService │     │MercadoPagoQRGateway│   │MercadoPago │
└───┬────┘     └────────┬─────────┘     └───────┬───────┘     └────────┬─────────┘     └─────┬──────┘
    │                   │                       │                      │                     │
    │ Click "Pagar QR"  │                       │                      │                     │
    │──────────────────>│                       │                      │                     │
    │                   │                       │                      │                     │
    │                   │ StartQrAsync(items)   │                      │                     │
    │                   │──────────────────────>│                      │                     │
    │                   │                       │                      │                     │
    │                   │                       │ Crear Order local    │                     │
    │                   │                       │──────────┐           │                     │
    │                   │                       │          │           │                     │
    │                   │                       │<─────────┘           │                     │
    │                   │                       │                      │                     │
    │                   │                       │ CreateQrOrderAsync() │                     │
    │                   │                       │─────────────────────>│                     │
    │                   │                       │                      │                     │
    │                   │                       │                      │ POST /instore/orders/qr/...
    │                   │                       │                      │────────────────────>│
    │                   │                       │                      │                     │
    │                   │                       │                      │  {qr_data, in_store_order_id}
    │                   │                       │                      │<────────────────────│
    │                   │                       │                      │                     │
    │                   │                       │<─────────────────────│                     │
    │                   │                       │                      │                     │
    │                   │                       │ Generar imagen QR    │                     │
    │                   │                       │──────────┐           │                     │
    │                   │                       │          │           │                     │
    │                   │                       │<─────────┘           │                     │
    │                   │                       │                      │                     │
    │                   │ StartQrResponse       │                      │                     │
    │                   │<──────────────────────│                      │                     │
    │                   │                       │                      │                     │
    │  HTML con QR      │                       │                      │                     │
    │<──────────────────│                       │                      │                     │
    │                   │                       │                      │                     │
```

### 4.2 Endpoint de Mercado Pago

**URL:** `POST /instore/orders/qr/seller/collectors/{user_id}/pos/{external_pos_id}/qrs`

**Archivo:** `Infrastructure/Gateways/MercadoPago/MercadoPagoQRGateway/MercadoPagoQRGateway.cs:37-41`

```csharp
/// <summary>
/// Construye la URL del endpoint de QR de Mercado Pago.
/// La URL incluye el UserId del vendedor y el ExternalPosId del punto de venta.
/// </summary>
private string GetUrlPostQR()
{
    // Estructura: /instore/orders/qr/seller/collectors/{user_id}/pos/{external_pos_id}/qrs
    var url = $"{MercadoPagoApiBaseUrl}/instore/orders/qr/seller/collectors/{_mercadoPagoQrOptions.UserId}/pos/{_mercadoPagoQrOptions.ExternalPosId}/qrs";
    return url;
}
```

### 4.3 Estructura del Request

**DTO:** `Infrastructure/Gateways/MercadoPago/DTO/QrDTO/QrOrderRequest.cs`

```csharp
/// <summary>
/// Request para crear una orden QR dinámica en Mercado Pago.
/// Todos los campos usan snake_case para coincidir con la API de MP.
/// </summary>
public sealed record QrOrderRequest
{
    /// <summary>
    /// Referencia externa que vincula el pago con nuestro sistema.
    /// CRÍTICO: Este es nuestro OrderId local. MP lo devuelve en el webhook
    /// dentro del campo external_reference de la merchant_order.
    /// </summary>
    [JsonPropertyName("external_reference")]
    public string ExternalReference { get; init; }

    /// <summary>
    /// Título visible en la app de MP cuando el usuario escanea.
    /// </summary>
    [JsonPropertyName("title")]
    public string Title { get; init; }

    /// <summary>
    /// Descripción de la compra.
    /// </summary>
    [JsonPropertyName("description")]
    public string Description { get; init; }

    /// <summary>
    /// URL donde MP enviará las notificaciones (webhooks).
    /// Debe ser accesible desde internet (usar ngrok en desarrollo).
    /// </summary>
    [JsonPropertyName("notification_url")]
    public string NotificationUrl { get; init; }

    /// <summary>
    /// Monto total de la orden en enteros (sin decimales para ARS).
    /// </summary>
    [JsonPropertyName("total_amount")]
    public int TotalAmount { get; init; }

    /// <summary>
    /// Lista de items de la orden.
    /// </summary>
    [JsonPropertyName("items")]
    public List<QrOrderItemRequest> Items { get; set; }
}
```

### 4.4 La Importancia del external_reference

El `external_reference` es **el campo más crítico** de la integración:

1. **Al crear la orden:** Enviamos nuestro `OrderId` local como `external_reference`
2. **En el webhook:** MP nos envía el `merchant_order_id` (ID de MP, no el nuestro)
3. **Para reconciliar:** Consultamos `/merchant_orders/{id}` y extraemos el `external_reference`
4. **Actualizamos:** Encontramos nuestra orden local usando ese ID

**Archivo:** `Application/Services/PaymentService/PaymentService.cs:129-151`

```csharp
/// <summary>
/// Crea el objeto QrOrderRequest a partir de una Order local.
/// El external_reference es el OrderId que nos permitirá identificar
/// el pago cuando llegue el webhook.
/// </summary>
private QrOrderRequest CreateQrOrder(Order order)
{
    return new QrOrderRequest
    {
        // CRÍTICO: Este es el vínculo entre MP y nuestro sistema
        ExternalReference = order.Id,

        Title = order.Title,
        Description = $"Compra de {order.Items.Count} productos",

        // URL del webhook - MP enviará notificaciones aquí
        NotificationUrl = $"{_mercadoPagoQrOptions.BaseUrl}/webhooks/mercadopago/qr",

        // El total debe ser entero para ARS
        TotalAmount = (int)order.Total,

        Items = order.Items.Select(item => new QrOrderItemRequest
        {
            SkuNumber = item.ProductId,
            Category = "general",
            Title = item.Title,
            Description = item.Title,
            UnitPrice = (int)item.UnitPrice,
            Quantity = item.Quantity,
            UnitMeasure = "unit",
            TotalAmount = (int)item.SubTotal
        }).ToList()
    };
}
```

### 4.5 Response de Mercado Pago

**DTO:** `Infrastructure/Gateways/MercadoPago/DTO/QrDTO/QrOrderResponse.cs`

```csharp
/// <summary>
/// Response al crear una orden QR dinámica.
/// </summary>
public sealed record QrOrderResponse
{
    /// <summary>
    /// String EMV que codifica toda la información del pago.
    /// Este string se usa para generar la imagen QR que el cliente escanea.
    /// Formato: EMV Co-standard (usado por billeteras digitales).
    /// </summary>
    [JsonPropertyName("qr_data")]
    public string QrData { get; set; }

    /// <summary>
    /// ID de la orden en el sistema de MP (in-store).
    /// NO confundir con nuestro OrderId ni con merchant_order_id.
    /// </summary>
    [JsonPropertyName("in_store_order_id")]
    public string InStoreOrderId { get; set; }
}
```

### 4.6 Generación de la Imagen QR

**Archivo:** `Infrastructure/QRCode/QrCodeGenerator.cs`

```csharp
/// <summary>
/// Genera una imagen QR en formato PNG codificada en Base64.
/// Usa la librería QRCoder para crear el QR desde el string EMV de MP.
/// </summary>
public class QrCodeGenerator : IQrCodeGenerator
{
    /// <summary>
    /// Convierte el qr_data de MP en una imagen QR lista para mostrar en HTML.
    /// </summary>
    /// <param name="qrData">String EMV retornado por MP</param>
    /// <param name="pixelsPerModule">Tamaño del QR (default: 20px por módulo)</param>
    /// <returns>Data URI: "data:image/png;base64,..." listo para <img src="..."></returns>
    public string GenerateQrImageBase64(string qrData, int pixelsPerModule = 20)
    {
        // Crear generador de QR
        using var qrGenerator = new QRCodeGenerator();

        // ECC Level M = 15% de corrección de errores
        // Balance entre tamaño del QR y tolerancia a daños
        using var qrCodeData = qrGenerator.CreateQrCode(qrData, ECCLevel.M);

        // Render a PNG
        using var qrCode = new PngByteQRCode(qrCodeData);
        byte[] qrCodeBytes = qrCode.GetGraphic(pixelsPerModule);

        // Convertir a Base64 con prefijo data URI
        string base64 = Convert.ToBase64String(qrCodeBytes);
        return $"data:image/png;base64,{base64}";
    }
}
```

---

## 5. Verificación del Pago (El Cierre)

### 5.1 Diagrama del Flujo de Webhook

```
┌────────────┐     ┌──────────────────────┐     ┌───────────────┐     ┌──────────────────┐
│MercadoPago │     │WebhookController     │     │PaymentService │     │MercadoPagoQRGateway│
└─────┬──────┘     └──────────┬───────────┘     └───────┬───────┘     └────────┬─────────┘
      │                       │                         │                      │
      │ POST /webhooks/mercadopago/qr                   │                      │
      │ ?topic=merchant_order&id=37461186157            │                      │
      │──────────────────────>│                         │                      │
      │                       │                         │                      │
      │                       │ ProcessMerchantOrderWebhookAsync(37461186157)  │
      │                       │────────────────────────>│                      │
      │                       │                         │                      │
      │                       │                         │ GetMerchantOrderStatusAsync()
      │                       │                         │─────────────────────>│
      │                       │                         │                      │
      │                       │                         │                      │ GET /merchant_orders/37461186157
      │                       │                         │                      │─────────────────────────────────>│
      │                       │                         │                      │                                  │
      │                       │                         │                      │ {status, external_reference, ...}│
      │                       │                         │                      │<─────────────────────────────────│
      │                       │                         │                      │
      │                       │                         │<─────────────────────│
      │                       │                         │                      │
      │                       │                         │ Buscar orden local por external_reference
      │                       │                         │ Actualizar estado
      │                       │                         │────────────┐         │
      │                       │                         │            │         │
      │                       │                         │<───────────┘         │
      │                       │                         │                      │
      │                       │ QrPaymentStatusDTO      │                      │
      │                       │<────────────────────────│                      │
      │                       │                         │                      │
      │                       │ NotifyPaymentCompletedAsync() ──> SignalR ──> Cliente
      │                       │                         │                      │
      │  HTTP 200 OK          │                         │                      │
      │<──────────────────────│                         │                      │
```

### 5.2 Webhook Controller

**Archivo:** `Presentation/WebHooks/MercadopagoWebhookController.cs:64-119`

```csharp
/// <summary>
/// Endpoint que recibe las notificaciones de Mercado Pago para pagos QR.
/// MP envía el merchant_order_id, NO nuestro OrderId.
/// </summary>
[HttpPost("webhooks/mercadopago/qr")]
public async Task<IActionResult> ReceiveQrWebhook(
    [FromQuery] string? topic,      // "merchant_order" para QR
    [FromQuery] string? type,       // Alternativa a topic
    [FromQuery] long? id,           // merchant_order_id
    [FromQuery(Name = "data.id")] long? dataId  // Formato alternativo
)
{
    _logger.LogInformation(
        "Webhook QR recibido - Topic: {Topic}, Type: {Type}, Id: {Id}, DataId: {DataId}",
        topic, type, id, dataId
    );

    // MP puede enviar como "topic" o "type"
    var notificationType = topic ?? type;
    var merchantOrderId = id ?? dataId;

    // Solo procesamos merchant_order (no payment para QR)
    if (notificationType != "merchant_order" || !merchantOrderId.HasValue)
    {
        _logger.LogWarning("Webhook QR ignorado - no es merchant_order");
        return Ok();
    }

    // IMPORTANTE: El merchantOrderId es el ID de MP, no nuestro OrderId
    // ProcessMerchantOrderWebhookAsync consulta a MP para obtener el external_reference
    var qrPaymentStatus = await _paymentService.ProcessMerchantOrderWebhookAsync(merchantOrderId.Value);

    if (qrPaymentStatus is null)
    {
        _logger.LogError("No se pudo procesar merchant_order {Id}", merchantOrderId);
        return Ok();  // Siempre retornar 200 para evitar reintentos
    }

    // Notificar al cliente vía SignalR
    await _signalRNotificacionService.NotifyPaymentCompletdedAsync(
        qrPaymentStatus.OrderId,
        qrPaymentStatus.Status,
        qrPaymentStatus.PaymentId
    );

    return Ok();
}
```

### 5.3 Diferencia: Webhook vs Consulta a Merchant Order

| Dato | Viene en el Webhook | Viene de GET /merchant_orders/{id} |
|------|---------------------|-----------------------------------|
| merchant_order_id | ✅ Sí (como `id`) | ✅ Sí (como `id`) |
| external_reference (OrderId) | ❌ No | ✅ Sí |
| status | ❌ No | ✅ Sí (`opened`, `closed`, etc.) |
| paid_amount | ❌ No | ✅ Sí |
| total_amount | ❌ No | ✅ Sí |
| payments[] | ❌ No | ✅ Sí (array con payment_id) |

**Conclusión:** El webhook solo avisa que algo cambió. Siempre debes consultar `/merchant_orders/{id}` para obtener los datos reales.

### 5.4 Consulta de Merchant Order

**Archivo:** `Infrastructure/Gateways/MercadoPago/MercadoPagoQRGateway/MercadoPagoQRGateway.cs:111-155`

```csharp
/// <summary>
/// Consulta el estado de una merchant order en Mercado Pago.
/// Esta consulta nos da el external_reference (OrderId) y el estado real.
/// </summary>
public async Task<MerchantOrderStatusResponse> GetMerchantOrderStatusAsync(
    string inStoreOrderId,
    CancellationToken cancellationToken = default)
{
    // Construir URL: GET /merchant_orders/{id}
    var url = GetUrlGetMerchantOrder(inStoreOrderId);

    var httpClient = GetHttpClient();
    var response = await httpClient.GetAsync(url, cancellationToken);
    var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

    if (!response.IsSuccessStatusCode)
    {
        _logger.LogError("Error al consultar merchant order. StatusCode: {Code}", response.StatusCode);
        throw new HttpRequestException($"Error: {response.StatusCode}");
    }

    return JsonSerializer.Deserialize<MerchantOrderStatusResponse>(responseBody,
        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
}
```

### 5.5 DTO de Merchant Order

**Archivo:** `Infrastructure/Gateways/MercadoPago/DTO/QrDTO/MerchantOrderStatusResponse.cs`

```csharp
/// <summary>
/// Response al consultar GET /merchant_orders/{id}.
/// Contiene toda la información necesaria para validar el pago.
/// </summary>
public sealed record MerchantOrderStatusResponse
{
    /// <summary>
    /// ID de la merchant order en MP (el que llega en el webhook).
    /// </summary>
    [JsonPropertyName("id")]
    public long Id { get; init; }

    /// <summary>
    /// Estado de la orden. Valores posibles:
    /// - "opened": Esperando pago (QR generado pero no pagado)
    /// - "closed": Pagada completamente (PaidAmount >= TotalAmount)
    /// - "expired": Expirada sin pago
    /// </summary>
    [JsonPropertyName("status")]
    public string Status { get; init; }

    /// <summary>
    /// NUESTRO OrderId. Esto es lo que enviamos como external_reference al crear el QR.
    /// Este es el campo que usamos para encontrar la orden en nuestra base de datos.
    /// </summary>
    [JsonPropertyName("external_reference")]
    public string ExternalReference { get; init; }

    /// <summary>
    /// Monto total de la orden.
    /// </summary>
    [JsonPropertyName("total_amount")]
    public decimal TotalAmount { get; init; }

    /// <summary>
    /// Monto ya pagado.
    /// Validación: PaidAmount >= TotalAmount significa pago completo.
    /// </summary>
    [JsonPropertyName("paid_amount")]
    public decimal PaidAmount { get; init; }

    /// <summary>
    /// Lista de pagos asociados. Si hay pagos, el primero contiene el payment_id.
    /// </summary>
    [JsonPropertyName("payments")]
    public List<MerchantOrderPayment> Payments { get; init; } = [];
}
```

### 5.6 Lógica de Validación del Pago

**Archivo:** `Application/Services/PaymentService/PaymentService.cs:292-358`

```csharp
/// <summary>
/// Procesa una notificación de merchant order desde el webhook.
/// Este es el método clave que reconcilia el pago de MP con nuestra orden local.
/// </summary>
public async Task<QrPaymentStatusDTO> ProcessMerchantOrderWebhookAsync(
    long merchantOrderId,
    CancellationToken cancellationToken = default)
{
    // 1. Consultar a MercadoPago por el merchant_order_id
    var merchantOrderStatus = await _qRGateway.GetMerchantOrderStatusAsync(
        merchantOrderId.ToString(),
        cancellationToken
    );

    if (merchantOrderStatus is null)
    {
        _logger.LogWarning("No se pudo obtener merchant order {Id}", merchantOrderId);
        return null;
    }

    // 2. Extraer el external_reference (nuestro OrderId local)
    var localOrderId = merchantOrderStatus.ExternalReference;

    if (string.IsNullOrEmpty(localOrderId))
    {
        _logger.LogWarning("Merchant order {Id} no tiene external_reference", merchantOrderId);
        return null;
    }

    // 3. Buscar la orden local usando el external_reference
    var order = await _orderRepository.GetByIdAsync(localOrderId);

    if (order is null)
    {
        _logger.LogWarning("Orden local {Id} no encontrada", localOrderId);
        return null;
    }

    // 4. Mapear estado de MP a estado local
    // Estados de merchant_order: opened, closed, expired
    // "closed" significa PaidAmount >= TotalAmount
    var newStatus = MapMerchantOrderStatusToOrderStatus(merchantOrderStatus.Status);

    // 5. Actualizar si cambió el estado
    if (order.Status != newStatus)
    {
        order.Status = newStatus;

        // Guardar el payment_id si hay pagos
        if (merchantOrderStatus.Payments.Count > 0)
        {
            order.MercadoPagoPaymentId = merchantOrderStatus.Payments.First().Id;
        }

        await _orderRepository.UpdateAsync(order);
    }

    return new QrPaymentStatusDTO
    {
        OrderId = order.Id,
        InStoreOrderId = order.MercadoPagoInStoreOrderId,
        Status = merchantOrderStatus.Status,  // "closed" = pagado
        PaymentId = order.MercadoPagoPaymentId,
        Total = order.Total
    };
}

/// <summary>
/// Mapea el estado de merchant order de MP al estado de orden local.
/// </summary>
private OrderStatus MapMerchantOrderStatusToOrderStatus(string? merchantOrderStatus)
{
    return merchantOrderStatus?.ToLower() switch
    {
        "closed" => OrderStatus.approved,      // Pagado completamente
        "paid" => OrderStatus.approved,        // Alternativa (menos común)
        "opened" => OrderStatus.Pending,       // Esperando pago
        "partially_paid" => OrderStatus.Pending,
        "cancelled" => OrderStatus.Rejected,
        "expired" => OrderStatus.Rejected,
        _ => OrderStatus.Pending
    };
}
```

---

## 6. Notificaciones en Tiempo Real (SignalR)

### 6.1 Arquitectura SignalR

```
┌──────────────────────────────────────────────────────────────────────┐
│                           SERVIDOR                                    │
│  ┌─────────────────────────────────────────────────────────────────┐ │
│  │ PaymentNotificationHub                                          │ │
│  │  - Gestiona grupos por orderId: "order_{orderId}"               │ │
│  │  - Los clientes se suscriben al grupo de su orden               │ │
│  └─────────────────────────────────────────────────────────────────┘ │
│  ┌─────────────────────────────────────────────────────────────────┐ │
│  │ PaymentNotificationService                                      │ │
│  │  - Envía notificaciones a grupos específicos                    │ │
│  │  - Llamado desde el WebhookController cuando llega un pago      │ │
│  └─────────────────────────────────────────────────────────────────┘ │
└──────────────────────────────────────────────────────────────────────┘
                                    │
                                    │ WebSocket
                                    │
┌──────────────────────────────────────────────────────────────────────┐
│                           CLIENTE                                     │
│  ┌─────────────────────────────────────────────────────────────────┐ │
│  │ qr-payment.js (QrPaymentNotificationClient)                     │ │
│  │  - Se conecta a /hubs/payment-notification                      │ │
│  │  - Se une al grupo "order_{orderId}"                            │ │
│  │  - Escucha evento "PaymentCompleted"                            │ │
│  │  - Redirige al usuario cuando el pago es exitoso                │ │
│  └─────────────────────────────────────────────────────────────────┘ │
└──────────────────────────────────────────────────────────────────────┘
```

### 6.2 Hub SignalR (Servidor)

**Archivo:** `Infrastructure/SignalR/Hub/PaymentNotificationHub.cs`

```csharp
/// <summary>
/// Interfaz tipada para los métodos que el servidor puede llamar en el cliente.
/// SignalR usará esta interfaz para invocar "PaymentCompleted" en los clientes.
/// </summary>
public interface IPaymentNotificationClient
{
    /// <summary>
    /// Método invocado cuando un pago se completa.
    /// El cliente JavaScript debe tener un handler para "PaymentCompleted".
    /// </summary>
    Task PaymentCompleted(PaymentCompletedNotification notification);
}

/// <summary>
/// Hub de SignalR para notificaciones de pago en tiempo real.
/// Usa el patrón de grupos para enviar notificaciones solo a clientes
/// interesados en una orden específica.
/// </summary>
public class PaymentNotificationHub : Hub<IPaymentNotificationClient>
{
    /// <summary>
    /// Permite al cliente unirse al grupo de una orden específica.
    /// El cliente llama esto después de conectarse, pasando el orderId.
    /// </summary>
    /// <param name="orderId">ID de la orden que el cliente quiere monitorear</param>
    public async Task JoinOrderGroup(string orderId)
    {
        if (string.IsNullOrWhiteSpace(orderId))
        {
            _logger.LogWarning("Intento de unirse a grupo con orderId vacío");
            return;
        }

        // El nombre del grupo incluye prefijo para evitar colisiones
        await Groups.AddToGroupAsync(Context.ConnectionId, $"order_{orderId}");

        _logger.LogInformation(
            "Connection {ConnectionId} joined group order_{OrderId}",
            Context.ConnectionId,
            orderId
        );
    }
}
```

### 6.3 Servicio de Notificación

**Archivo:** `Infrastructure/SignalR/NotificationService/PaymentNotificacionService.cs`

```csharp
/// <summary>
/// Servicio que envía notificaciones de pago a clientes conectados vía SignalR.
/// Es llamado desde el WebhookController después de procesar un webhook de MP.
/// </summary>
public class PaymentNotificationService : IPaymentNotificationService
{
    private readonly IHubContext<PaymentNotificationHub, IPaymentNotificationClient> _hubContext;

    /// <summary>
    /// Genera un mensaje amigable según el estado del pago.
    /// </summary>
    private static string GenerateMessage(string? status)
    {
        return status?.ToLower() switch
        {
            // "closed" es el estado de merchant_order cuando está pagada
            "approved" or "paid" or "closed" => "¡Pago aprobado exitosamente!",
            "rejected" => "El pago fue rechazado",
            "pending" or "opened" => "El pago está siendo procesado",
            "cancelled" => "El pago fue cancelado",
            _ => "El estado del pago ha sido actualizado"
        };
    }

    /// <summary>
    /// Envía una notificación de pago completado a todos los clientes
    /// suscritos al grupo de la orden.
    /// </summary>
    public async Task NotifyPaymentCompletdedAsync(string orderId, string status, long? payment_id)
    {
        if (string.IsNullOrWhiteSpace(orderId)) return;

        // Nombre del grupo que coincide con el usado en JoinOrderGroup
        var groupName = $"order_{orderId}";

        var notification = new PaymentCompletedNotification
        {
            OrderId = orderId,
            Status = status ?? "unknown",
            PaymentId = payment_id,
            Timestamp = DateTimeOffset.UtcNow,
            Message = GenerateMessage(status)
        };

        // Enviar a todos los clientes del grupo
        await _hubContext.Clients.Group(groupName).PaymentCompleted(notification);

        _logger.LogInformation(
            "Notificación enviada al grupo {Group}: Status={Status}",
            groupName,
            status
        );
    }
}
```

### 6.4 Cliente SignalR (JavaScript)

**Archivo:** `wwwroot/js/qr-payment.js`

```javascript
/**
 * Clase que maneja la conexión SignalR para recibir
 * notificaciones de pago en tiempo real.
 *
 * Flujo:
 * 1. Se instancia cuando se muestra el QR al usuario
 * 2. Se conecta al hub en /hubs/payment-notification
 * 3. Se une al grupo de la orden (JoinOrderGroup)
 * 4. Espera el evento "PaymentCompleted"
 * 5. Redirige al usuario según el resultado
 */
class QrPaymentNotificationClient {
    constructor() {
        this.connection = null;      // Conexión SignalR
        this.currendOrderId = null;  // Orden que estamos monitoreando
        this.onPaymentCompleted = null;
        this.onError = null;
    }

    /**
     * Inicia la conexión SignalR y se suscribe a una orden.
     * @param {string} orderId - ID de la orden a monitorear
     * @param {Function} onPaymentCompleted - Callback cuando el pago se completa
     * @param {Function} onError - Callback para errores
     */
    async start(orderId, onPaymentCompleted, onError = null) {
        this.currendOrderId = orderId;
        this.onPaymentCompleted = onPaymentCompleted;
        this.onError = onError;

        // Crear conexión al Hub
        // La URL debe coincidir con app.MapHub<>() en Program.cs
        this.connection = new signalR.HubConnectionBuilder()
            .withUrl("/hubs/payment-notification")
            .withAutomaticReconnect()  // Reconexión automática
            .configureLogging(signalR.LogLevel.Information)
            .build();

        // Registrar handlers ANTES de conectar
        this._registerEventHandlers();

        // Iniciar conexión
        await this.connection.start();
        console.log("Conexión SignalR iniciada.");

        // Unirse al grupo de la orden
        await this.connection.invoke("JoinOrderGroup", orderId);
        console.log(`Unido al grupo de la orden ${orderId}`);
    }

    /**
     * Registra los manejadores de eventos del servidor.
     */
    _registerEventHandlers() {
        // Handler para "PaymentCompleted" - el servidor llama esto
        this.connection.on("PaymentCompleted", (notification) => {
            console.log("Notificación de pago recibida:", notification);
            // notification = { orderId, status, paymentId, timestamp, message }
            if (this.onPaymentCompleted) {
                this.onPaymentCompleted(notification);
            }
        });

        // Manejar reconexión - volver a unirse al grupo
        this.connection.onreconnected(async (connectionId) => {
            if (this.currendOrderId) {
                await this.connection.invoke("JoinOrderGroup", this.currendOrderId);
            }
        });
    }

    async stop() {
        if (this.connection) {
            await this.connection.stop();
        }
    }
}

// Instancia global
let qrPaymentClient = null;

/**
 * Función pública llamada cuando se muestra el QR.
 * Inicializa SignalR y configura los callbacks.
 */
function startQrPaymentNotificationClient(orderId) {
    if (!qrPaymentClient) {
        qrPaymentClient = new QrPaymentNotificationClient();
    }

    const handlePaymentCompleted = (notification) => {
        const status = notification.status.toLowerCase();

        // Mostrar mensaje al usuario
        if (notification.message) {
            showToast(notification.message,
                status === 'approved' || status === 'paid' || status === 'closed'
                    ? 'success' : 'info');
        }

        // Redirigir según el resultado
        // IMPORTANTE: "closed" es el status de merchant_order cuando está pagada
        if (status === 'approved' || status === 'paid' || status === 'closed') {
            setTimeout(() => {
                window.location.href = `/checkout/return/success?payment_id=${notification.paymentId || ''}&status=${notification.status}`;
            }, 2000);
        } else if (status === 'rejected' || status === 'cancelled') {
            setTimeout(() => {
                window.location.href = `/checkout/return/failure?status=${notification.status}`;
            }, 2000);
        }
    };

    qrPaymentClient.start(orderId, handlePaymentCompleted, handleError);
}

/**
 * Limpia la conexión SignalR.
 * Llamado cuando el usuario cancela o sale de la página.
 */
function cleanupQrPaymentNotification() {
    if (qrPaymentClient) {
        qrPaymentClient.stop();
        qrPaymentClient = null;
    }
}
```

### 6.5 Inicialización desde cart.js

**Archivo:** `wwwroot/js/cart.js:359-421`

```javascript
/**
 * Inicia el proceso de checkout con QR Dinámico.
 * Llama al endpoint /MercadoPago/paymentQr y carga la vista del QR.
 *
 * IMPORTANTE: Los scripts dentro de innerHTML NO se ejecutan.
 * Por eso inicializamos SignalR manualmente después de cargar el HTML.
 */
function proceedToQr() {
    const cartBody = document.getElementById('cart-offcanvas-body');

    // Mostrar loading
    cartBody.innerHTML = `<div class="text-center py-5">
        <div class="spinner-border text-primary" role="status"></div>
        <p class="mt-3">Generando código QR...</p>
    </div>`;

    // Llamar al endpoint
    fetch('/MercadoPago/paymentQr', {
        method: 'POST',
        body: formData
    })
    .then(response => response.text())
    .then(html => {
        // Cargar el HTML del QR
        cartBody.innerHTML = html;

        // CRÍTICO: Los <script> dentro de innerHTML NO se ejecutan
        // Debemos inicializar SignalR manualmente
        const qrContent = document.getElementById('checkout-qr-content');
        if (qrContent) {
            const orderId = qrContent.getAttribute('data-order-id');
            if (orderId && typeof startQrPaymentNotificationClient === 'function') {
                console.log('Inicializando SignalR para orden:', orderId);
                startQrPaymentNotificationClient(orderId);
            }
        }
    });
}

/**
 * Cancela el checkout QR y vuelve al carrito.
 */
function cancelQrCheckout() {
    // Limpiar conexión SignalR
    if (typeof cleanupQrPaymentNotification === 'function') {
        cleanupQrPaymentNotification();
    }
    // Recargar carrito
    loadCartContent();
}
```

---

## 7. Guía de Uso Rápido

### 7.1 Configuración Inicial (Una sola vez)

1. **Crear Test Users en MP:**
   - Ir a https://www.mercadopago.com.ar/developers/panel/test-users
   - Crear un usuario "Vendedor" y un usuario "Comprador"

2. **Crear Sucursal y Caja:**
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

3. **Configurar appsettings.json:**
   ```json
   {
     "MercadoPagoQr": {
       "AccessToken": "TOKEN_DEL_VENDEDOR",
       "PublicKey": "PUBLIC_KEY_DEL_VENDEDOR",
       "UserId": "ID_DEL_VENDEDOR",
       "ExternalPosId": "SUC001POS001",
       "BaseUrl": "https://tu-dominio.ngrok.dev"
     }
   }
   ```

4. **Iniciar ngrok para webhooks:**
   ```bash
   ngrok http 5000
   # Copiar la URL https://xxx.ngrok.dev a BaseUrl
   ```

### 7.2 Flujo de Prueba Paso a Paso

```
┌─────────────────────────────────────────────────────────────────────┐
│ PASO 1: Agregar productos al carrito                                │
│ - Abrir la aplicación en el navegador                               │
│ - Agregar uno o más productos al carrito                            │
└─────────────────────────────────────────────────────────────────────┘
                                    │
                                    ▼
┌─────────────────────────────────────────────────────────────────────┐
│ PASO 2: Iniciar pago con QR                                         │
│ - Abrir el carrito (offcanvas)                                      │
│ - Click en "Pagar con QR"                                           │
│ - Esperar que se genere el código QR                                │
└─────────────────────────────────────────────────────────────────────┘
                                    │
                                    ▼
┌─────────────────────────────────────────────────────────────────────┐
│ PASO 3: Escanear con la app de MP (Test User Comprador)             │
│ - Abrir app Mercado Pago en el celular (logueado como comprador)    │
│ - Ir a "Escanear QR" o "Pagar"                                      │
│ - Escanear el código QR mostrado en pantalla                        │
│ - Confirmar el pago en la app                                       │
└─────────────────────────────────────────────────────────────────────┘
                                    │
                                    ▼
┌─────────────────────────────────────────────────────────────────────┐
│ PASO 4: Verificar resultado                                         │
│ - El webhook llega a /webhooks/mercadopago/qr                       │
│ - SignalR notifica al cliente                                       │
│ - El usuario es redirigido a /checkout/return/success               │
└─────────────────────────────────────────────────────────────────────┘
```

### 7.3 Verificación de Logs

Durante la prueba, verificar en la consola del servidor:

```
info: Webhook QR recibido - Topic: merchant_order, Id: 37461186157
info: Orden local abc123 actualizada a estado approved
info: Notificación enviada al grupo order_abc123: Status=closed
```

Y en la consola del navegador (F12):

```
Inicializando SignalR para orden: abc123
Conexión SignalR iniciada.
Unido al grupo de la orden abc123
Notificación de pago recibida: {orderId: "abc123", status: "closed", ...}
```

---

## 8. Referencia de Código

### 8.1 Estructura de Archivos

```
poc_mercadopago/
├── Application/
│   ├── DTOs/
│   │   └── StartQrDTO/
│   │       ├── StartQrRequest.cs      # Request para iniciar pago QR
│   │       ├── StartQrResponse.cs     # Response con datos del QR
│   │       └── QrPaymentStatusDTO.cs  # Estado del pago QR
│   └── Services/
│       └── PaymentService/
│           ├── IPaymentService.cs     # Interfaz con métodos QR
│           └── PaymentService.cs      # Implementación
│
├── Infrastructure/
│   ├── Gateways/
│   │   └── MercadoPago/
│   │       ├── Configuration/
│   │       │   └── MercadoPagoQrOptions.cs  # Configuración QR
│   │       ├── DTO/
│   │       │   └── QrDTO/
│   │       │       ├── QrOrderRequest.cs         # Request a MP
│   │       │       ├── QrOrderItemRequest.cs     # Item del request
│   │       │       ├── QrOrderResponse.cs        # Response de MP
│   │       │       └── MerchantOrderStatusResponse.cs # Estado de orden
│   │       └── MercadoPagoQRGateway/
│   │           ├── IMercadoPagoQRGateway.cs  # Interfaz
│   │           └── MercadoPagoQRGateway.cs   # Comunicación con MP
│   ├── QRCode/
│   │   ├── IQrCodeGenerator.cs        # Interfaz
│   │   └── QrCodeGenerator.cs         # Genera imagen QR
│   └── SignalR/
│       ├── DTO/
│       │   └── PaymentCompletedNotification.cs  # Notificación
│       ├── Hub/
│       │   └── PaymentNotificationHub.cs        # Hub SignalR
│       └── NotificationService/
│           ├── IPaymentNotificationService.cs   # Interfaz
│           └── PaymentNotificacionService.cs    # Envía notificaciones
│
├── Presentation/
│   ├── Controllers/
│   │   └── MercadoPagoController.cs   # Endpoint PaymentQr
│   ├── ViewModels/
│   │   └── QrViewModels/
│   │       └── QrViewModel.cs         # ViewModel para la vista
│   ├── Views/
│   │   └── Cart/
│   │       └── _Qr.cshtml             # Vista del QR
│   └── WebHooks/
│       └── MercadopagoWebhookController.cs  # Recibe webhooks
│
├── wwwroot/
│   └── js/
│       ├── cart.js           # proceedToQr(), cancelQrCheckout()
│       └── qr-payment.js     # Cliente SignalR
│
└── Program.cs                # Configuración de servicios y SignalR
```

### 8.2 Inyección de Dependencias (Program.cs)

```csharp
// Configuración de MercadoPago QR
builder.Services.Configure<MercadoPagoQrOptions>(
    builder.Configuration.GetSection(MercadoPagoQrOptions.SectionName)
);

// Validación al inicio
builder.Services.AddOptions<MercadoPagoQrOptions>()
    .BindConfiguration(MercadoPagoQrOptions.SectionName)
    .ValidateDataAnnotations()
    .ValidateOnStart();

// Gateway de QR
builder.Services.AddScoped<IMercadoPagoQRGateway, MercadoPagoQRGateway>();

// Generador de imágenes QR
builder.Services.AddSingleton<IQrCodeGenerator, QrCodeGenerator>();

// SignalR
builder.Services.AddSignalR();
builder.Services.AddScoped<IPaymentNotificationService, PaymentNotificationService>();

// Mapeo del Hub
app.MapHub<PaymentNotificationHub>("/hubs/payment-notification");
```

---

## 9. Troubleshooting

### 9.1 Error 403 Forbidden al crear orden QR

**Causa:** El `UserId` en configuración no coincide con el dueño del `AccessToken`.

**Solución:** Verificar que ambos valores pertenecen al mismo Test User vendedor.

### 9.2 Error "El user del sponsor y del collector deben ser de tipos iguales"

**Causa:** Se incluyó el campo `sponsor` en el request usando Test Users.

**Solución:** Omitir el campo `sponsor` completamente en ambiente de desarrollo.

### 9.3 Webhook no llega

**Causas posibles:**
1. ngrok no está corriendo o la URL cambió
2. La `BaseUrl` en appsettings no coincide con la URL de ngrok
3. El endpoint no retorna HTTP 200

**Solución:**
- Verificar que ngrok esté activo: `ngrok http 5000`
- Actualizar `BaseUrl` con la nueva URL
- Verificar logs del servidor

### 9.4 SignalR no notifica al cliente

**Causas posibles:**
1. El cliente no se unió al grupo correcto
2. El script de `qr-payment.js` no se ejecutó
3. Error de CORS

**Solución:**
- Verificar en consola del navegador que aparece "Unido al grupo de la orden xxx"
- Asegurarse que `startQrPaymentNotificationClient()` se llama después de cargar el HTML
- Verificar que SignalR está configurado en Program.cs

### 9.5 Estado "closed" no reconocido como pago exitoso

**Causa:** El código solo verificaba estados "approved" o "paid".

**Solución:** Para merchant orders, el estado de pago completo es `closed`:

```javascript
// En qr-payment.js
if (status === 'approved' || status === 'paid' || status === 'closed') {
    // Pago exitoso
}
```

```csharp
// En PaymentNotificacionService.cs
"approved" or "paid" or "closed" => "¡Pago aprobado exitosamente!",
```

---

## 10. Lecciones Aprendidas

### 10.1 Diferencias Clave vs Checkout Pro

| Aspecto | Checkout Pro | QR Dinámico |
|---------|--------------|-------------|
| Flujo | Redirige a MP | QR en tu app |
| Notificación tipo | `payment` | `merchant_order` |
| ID en webhook | `payment_id` | `merchant_order_id` |
| Status pagado | `approved` | `closed` |
| Consulta | `/v1/payments/{id}` | `/merchant_orders/{id}` |

### 10.2 El external_reference es Crítico

El `external_reference` es el **único vínculo** entre tu sistema y Mercado Pago:
- Lo envías al crear la orden QR
- MP lo devuelve en la merchant_order
- Es tu OrderId local
- Sin él, no puedes reconciliar pagos

### 10.3 Webhooks Solo Avisan

El webhook de `merchant_order` solo dice "algo cambió". Siempre debes:
1. Recibir el `merchant_order_id` del webhook
2. Consultar `GET /merchant_orders/{id}` para obtener datos reales
3. Extraer el `external_reference` para encontrar tu orden
4. Verificar `status == "closed"` y `paid_amount >= total_amount`

### 10.4 innerHTML No Ejecuta Scripts

Cuando cargas HTML dinámicamente con `innerHTML`, los `<script>` dentro **no se ejecutan**. Siempre inicializa JavaScript manualmente después de insertar el HTML.

### 10.5 Consistencia en Nombres de Configuración

El nombre de la sección en `appsettings.json` debe coincidir **exactamente** con `SectionName` en la clase de opciones. Un typo como `MercadoPagoQrCode` vs `MercadoPagoQr` causa errores de validación al inicio.

---

## Referencias

- [Documentación oficial MP - QR Dinámico](https://www.mercadopago.com.ar/developers/es/docs/qr-code/integration-configuration/qr-dynamic/integration)
- [API Reference - Instore Orders](https://www.mercadopago.com.ar/developers/es/reference/qr-dynamic/_instore_orders_qr_seller_collectors_user_id_pos_external_pos_id_qrs/post)
- [Merchant Orders](https://www.mercadopago.com.ar/developers/es/reference/merchant_orders/_merchant_orders_id/get)
- [Test Users](https://www.mercadopago.com.ar/developers/es/docs/your-integrations/test/accounts)
