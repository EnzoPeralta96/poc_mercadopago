namespace poc_mercadopago.Infrastructure.Webhooks.MercadoPago.DTOs
{
    /// <summary>
    /// Representa una notificación de webhook de Mercado Pago normalizada.
    /// 
    /// Mercado Pago puede enviar notificaciones con diferentes formatos según el origen:
    /// - Checkout Pro: type=payment, id={payment_id}
    /// - QR Dinámico: topic=merchant_order, id={merchant_order_id}
    /// - Otros: type={tipo}, id={resource_id}
    /// 
    /// Este DTO unifica todos los formatos en una estructura consistente.
    /// </summary>
    public sealed class WebhookNotification
    {
        /// <summary>
        /// Identificador único de esta notificación.
        /// Se genera combinando Type + ResourceId para garantizar unicidad.
        /// Usado para idempotencia (evitar procesar la misma notificación dos veces).
        /// </summary>
        /// <example>"payment_123456789"</example>
        public string NotificationId { get; init; } = string.Empty;

        public WebhookNotificationType Type { get; init; }

        // <summary>
        /// Tipo de notificación como string original de MP.
        /// Útil para logging y debugging.
        /// </summary>
        /// <example>"payment", "merchant_order"</example>        
        public string RawType { get; init; } = string.Empty;

        /// <summary>
        /// ID del recurso en Mercado Pago.
        /// - Para Payment: el payment_id
        /// - Para MerchantOrder: el merchant_order_id
        /// </summary>
        public long ResourceId { get; init; }

        /// <summary>
        /// Timestamp de cuando se recibió la notificación.
        /// </summary>
        public DateTimeOffset ReceivedAt { get; init; }

        /// <summary>
        /// Firma del webhook (header x-signature) si está presente.
        /// Null si MP no envió firma.
        /// </summary>
        public string? Signature { get; init; }

        /// <summary>
        /// Request ID del header x-request-id si está presente.
        /// Útil para correlacionar con logs de MP.
        /// </summary>
        public string? RequestId { get; init; }
        public string? SourceIp { get; init; }
        public string? UserAgent { get; init; }

        /// <summary>
        /// Query string completo del request (para validación de firma).
        /// </summary>
        public string? QueryString { get; init; }

        /// <summary>
        /// Indica si la notificación ya fue procesada anteriormente (duplicado).
        /// Se establece por el servicio de idempotencia.
        /// </summary>
        public bool IsDuplicate { get; set; }
        
        /// <summary>
        /// Indica si la firma del webhook es válida.
        /// Null si no se pudo validar.
        /// </summary>
        public bool? IsSignatureValid { get; set; }

        /// <summary>
        /// Tipo de app que originó la notificación (ej: "checkout", "qr").
        /// Viene del query param appType en la notification_url.
        /// </summary>
        public string? AppType { get; set; }
    }

    public enum WebhookNotificationType
    {
        //Desconocido
        Unknown = 0,

        //Checkout Pro
        Payment = 1,

        //Qr Dinamico
        MerchantOrder = 2,

        //Notificacion de reembolso
        Refund = 3,

        //Reclado - Disputa
        ChargeBack = 4,
    }
}