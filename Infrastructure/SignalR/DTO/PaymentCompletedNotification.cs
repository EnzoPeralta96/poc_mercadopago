namespace poc_mercadopago.Infrastructure.SignalR.DTO
{
    /// <summary>
    /// DTO que se envía al cliente vía SignalR cuando un pago cambia de estado.
    ///
    /// Este objeto es enviado desde PaymentNotificationService cuando llega
    /// un webhook de Mercado Pago y se serializa a JSON para el cliente JavaScript.
    ///
    /// En el cliente (qr-payment.js), se recibe como:
    /// connection.on("PaymentCompleted", (notification) => { ... });
    ///
    /// Y puede accederse como:
    /// notification.orderId, notification.status, notification.message, etc.
    /// </summary>
    public sealed record PaymentCompletedNotification
    {
        /// <summary>
        /// ID de la orden en nuestro sistema local.
        /// Es el mismo que se usó como external_reference al crear el QR.
        /// Se usa para identificar a qué grupo SignalR enviar la notificación.
        /// </summary>
        public required string OrderId { get; init; } = string.Empty;

        /// <summary>
        /// Estado del pago/orden desde Mercado Pago.
        ///
        /// Para QR dinámico (merchant_order):
        /// - "opened": QR generado, esperando pago
        /// - "closed": Pago completado exitosamente
        /// - "expired": QR expirado sin pago
        ///
        /// Para Checkout Pro (payment):
        /// - "approved": Pago aprobado
        /// - "pending": Pago pendiente
        /// - "rejected": Pago rechazado
        ///
        /// IMPORTANTE: El cliente JavaScript debe reconocer "closed" como éxito
        /// para el flujo de QR.
        /// </summary>
        public required string Status { get; init; } = string.Empty;

        /// <summary>
        /// ID del pago en Mercado Pago (payment_id).
        /// Puede ser null si aún no hay un pago asociado.
        /// Útil para mostrar en el comprobante o para consultas posteriores.
        /// </summary>
        public required long? PaymentId { get; init; }

        /// <summary>
        /// Fecha y hora de la notificación en UTC.
        /// Útil para auditoría y debugging.
        /// </summary>
        public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;

        /// <summary>
        /// Mensaje amigable para mostrar al usuario.
        /// Ejemplos:
        /// - "¡Pago aprobado exitosamente!"
        /// - "El pago fue rechazado"
        /// - "El pago está siendo procesado"
        ///
        /// El cliente puede mostrar esto en un toast/alert.
        /// </summary>
        public string Message { get; init; } = string.Empty;
    }
}