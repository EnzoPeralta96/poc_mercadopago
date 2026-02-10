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
        public string NotificationId {get; init;} = string.Empty;
        
        public WebhookNotificationType Type {get; init;}

        // <summary>
        /// Tipo de notificación como string original de MP.
        /// Útil para logging y debugging.
        /// </summary>
        /// <example>"payment", "merchant_order"</example>        
        public string RawType {get; init;} = string.Empty;
        public long ResourceId {get; init;}
        public DateTimeOffset ReceivedAt {get; init;}
        public string? Signature {get; init;}
        public string? RequestId {get; init;}
        public string? SourceIp {get; init;}
        public string? UserAgent {get; init;}
        public bool IsDuplicate {get; set;}
        public bool? IsSignaturaValid {get; set;}
    }

    public enum WebhookNotificationType
    {
        //Desconocido
        Unknow = 0,

        //Checkout Pro
        Payment = 1,

        //Qr Dinamico
        MerchantOrder = 2,

        //Notificacion de reembolso
        Refund = 3,

        //Reclado - Disputa
        ChargeBack = 4
    }
}