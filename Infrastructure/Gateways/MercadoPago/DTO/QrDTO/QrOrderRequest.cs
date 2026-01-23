using System.Text.Json.Serialization;

namespace poc_mercadopago.Infrastructure.Gateways.MercadoPago.DTO.QrDTO
{
    /// <summary>
    /// Request para crear una orden QR dinámica en Mercado Pago.
    /// Endpoint: POST /instore/orders/qr/seller/collectors/{user_id}/pos/{external_pos_id}/qrs
    ///
    /// Todos los campos usan JsonPropertyName con snake_case para coincidir
    /// con el formato esperado por la API de Mercado Pago.
    ///
    /// NOTA sobre "sponsor": En modo Sandbox/Test Users, NO incluir este campo.
    /// Si se incluye, genera el error "El user del sponsor y del collector deben ser de tipos iguales".
    /// El sponsor solo se usa en integraciones de marketplace certificadas.
    /// </summary>
    public sealed record QrOrderRequest
    {
        /// <summary>
        /// Referencia externa que vincula el pago con nuestro sistema.
        ///
        /// CRÍTICO: Este es nuestro OrderId local. Es el ÚNICO vínculo entre
        /// Mercado Pago y nuestro sistema. Cuando llega el webhook, MP devuelve
        /// este valor en el campo external_reference de la merchant_order.
        ///
        /// Flujo:
        /// 1. Creamos orden local con ID "abc123"
        /// 2. Enviamos external_reference: "abc123" a MP
        /// 3. Webhook llega con merchant_order_id: 37461186157
        /// 4. Consultamos GET /merchant_orders/37461186157
        /// 5. Extraemos external_reference: "abc123"
        /// 6. Encontramos nuestra orden local
        /// </summary>
        [JsonPropertyName("external_reference")]
        public string ExternalReference { get; init; } = string.Empty;

        /// <summary>
        /// Título de la orden, visible en la app de MP cuando el usuario escanea.
        /// Ejemplo: "Orden abc123def"
        /// </summary>
        [JsonPropertyName("title")]
        public string Title { get; init; } = string.Empty;

        /// <summary>
        /// Descripción de la compra, visible en la app de MP.
        /// Ejemplo: "Compra de 3 productos"
        /// </summary>
        [JsonPropertyName("description")]
        public string Description { get; init; } = string.Empty;

        /// <summary>
        /// URL donde MP enviará las notificaciones (webhooks) cuando el pago cambie de estado.
        ///
        /// IMPORTANTE:
        /// - Debe ser HTTPS
        /// - Debe ser accesible desde internet
        /// - En desarrollo, usar ngrok para exponer el servidor local
        ///
        /// MP enviará webhooks de tipo "merchant_order" a esta URL con el merchant_order_id.
        /// </summary>
        [JsonPropertyName("notification_url")]
        public string NotificationUrl { get; init; } = string.Empty;

        /// <summary>
        /// Monto total de la orden.
        ///
        /// IMPORTANTE: Para pesos argentinos (ARS), debe ser un entero (sin decimales).
        /// Ejemplo: $1500.00 se envía como 1500
        /// </summary>
        [JsonPropertyName("total_amount")]
        public int TotalAmount { get; init; }

        /// <summary>
        /// Lista de items de la orden con sus detalles.
        /// Cada item tiene: sku, título, precio unitario, cantidad, etc.
        /// </summary>
        [JsonPropertyName("items")]
        public List<QrOrderItemRequest> Items { get; set; }

    }


}