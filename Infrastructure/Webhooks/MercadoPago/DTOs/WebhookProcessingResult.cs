namespace poc_mercadopago.Infrastructure.Webhooks.MercadoPago.DTOs
{
    /// <summary>
    /// Representa el resultado del procesamiento de una notificación de webhook.
    /// 
    /// Este DTO se usa para:
    /// - Registrar el resultado en el log de auditoría
    /// - Tomar decisiones sobre reintentos
    /// - Debugging y monitoreo
    /// </summary>
    public sealed class WebhookProcessingResult
    {
        /// <summary>
        /// Indica si el procesamiento fue exitoso.
        /// </summary>
        public bool Success { get; init; }

        /// <summary>
        /// Estado del procesamiento.
        /// </summary>
        public WebhookProcessingStatus Status { get; init; }

        /// <summary>
        /// ID de la notificación procesada (para correlación).
        /// </summary>
        public string NotificationId { get; init; } = string.Empty;

        /// <summary>
        /// ID de la orden interna afectada (si aplica).
        /// </summary>
        public string? OrderId { get; init; }

        /// <summary>
        /// Nuevo estado de la orden después del procesamiento (si cambió).
        /// </summary>
        public string? NewOrderStatus { get; init; }

        /// <summary>
        /// Mensaje descriptivo del resultado.
        /// </summary>
        public string Message { get; init; } = string.Empty;

        /// <summary>
        /// Mensaje de error si falló.
        /// </summary>
        public string? ErrorMessage { get; init; }

        /// <summary>
        /// Excepción capturada si hubo error.
        /// No se serializa para evitar exponer detalles internos.
        /// </summary>
        public Exception? Exception { get; init; }

        /// <summary> 
        /// Tiempo que tomó procesar la notificación (para monitoreo de performance).
        /// </summary>
        public TimeSpan ProcessingTime { get; init; }

        /// <summary> 
        /// Timestamp de cuando se completó el procesamiento.
        /// </summary>
        public DateTimeOffset ProcessedAt { get; init; }

        /// <summary>
        /// Indica si se envió notificación SignalR.
        /// Solo aplica para notificaciones de QR.
        /// </summary>
        public bool SignalRNotificationSent { get; init; }

        /// <summary>
        /// Crea un resultado exitoso.
        /// </summary>
        public static WebhookProcessingResult Successful(
            string notificationId,
            string? orderId = null,
            string? newStatus = null,
            bool signalRSent = false,
            TimeSpan? processingTime = null)
        {
            return new WebhookProcessingResult
            {
                Success = true,
                Status = WebhookProcessingStatus.Processed,
                NotificationId = notificationId,
                OrderId = orderId,
                NewOrderStatus = newStatus,
                Message = "Notificación procesada exitosamente",
                ProcessedAt = DateTimeOffset.UtcNow,
                ProcessingTime = processingTime ?? TimeSpan.Zero,
                SignalRNotificationSent = signalRSent
            };
        }

        /// <summary>
        /// Crea un resultado para notificación duplicada (ya procesada).
        /// </summary>
        public static WebhookProcessingResult Duplicate(string notificationId)
        {
            return new WebhookProcessingResult
            {
                Success = true, // No es un error, simplemente ya se procesó
                Status = WebhookProcessingStatus.Duplicate,
                NotificationId = notificationId,
                Message = "Notificación ya fue procesada anteriormente (duplicado)",
                ProcessedAt = DateTimeOffset.UtcNow,
                ProcessingTime = TimeSpan.Zero
            };
        }

        /// <summary>
        /// Crea un resultado para notificación ignorada (tipo no soportado).
        /// </summary>
        public static WebhookProcessingResult Ignored(string notificationId, string reason)
        {
            return new WebhookProcessingResult
            {
                Success = true, // No es un error, simplemente no nos interesa
                Status = WebhookProcessingStatus.Ignored,
                NotificationId = notificationId,
                Message = $"Notificación ignorada: {reason}",
                ProcessedAt = DateTimeOffset.UtcNow,
                ProcessingTime = TimeSpan.Zero
            };
        }

        /// <summary>
        /// Crea un resultado para firma inválida.
        /// </summary>
        public static WebhookProcessingResult InvalidSignature(string notificationId)
        {
            return new WebhookProcessingResult
            {
                Success = false,
                Status = WebhookProcessingStatus.InvalidSignature,
                NotificationId = notificationId,
                Message = "Firma de webhook inválida",
                ErrorMessage = "La firma x-signature no coincide con el payload",
                ProcessedAt = DateTimeOffset.UtcNow,
                ProcessingTime = TimeSpan.Zero
            };
        }

        /// <summary>
        /// Crea un resultado de error.
        /// </summary>
        public static WebhookProcessingResult Failed(
            string notificationId,
            string errorMessage,
            Exception? exception = null,
            TimeSpan? processingTime = null)
        {
            return new WebhookProcessingResult
            {
                Success = false,
                Status = WebhookProcessingStatus.Failed,
                NotificationId = notificationId,
                Message = "Error al procesar notificación",
                ErrorMessage = errorMessage,
                Exception = exception,
                ProcessedAt = DateTimeOffset.UtcNow,
                ProcessingTime = processingTime ?? TimeSpan.Zero
            };
        }

        /// <summary>
        /// Crea un resultado para orden no encontrada.
        /// </summary>
        public static WebhookProcessingResult OrderNotFound(string notificationId, string orderId)
        {
            return new WebhookProcessingResult
            {
                Success = false,
                Status = WebhookProcessingStatus.OrderNotFound,
                NotificationId = notificationId,
                OrderId = orderId,
                Message = $"Orden no encontrada: {orderId}",
                ErrorMessage = "No se encontró la orden asociada a esta notificación",
                ProcessedAt = DateTimeOffset.UtcNow,
                ProcessingTime = TimeSpan.Zero
            };
        }


    }

    public enum WebhookProcessingStatus
    {
        Processed = 1,
        Duplicate = 2,
        Ignored = 3,
        InvalidSignature = 4,
        Failed = 5,
        OrderNotFound = 6,
        Pending = 7
    }


}