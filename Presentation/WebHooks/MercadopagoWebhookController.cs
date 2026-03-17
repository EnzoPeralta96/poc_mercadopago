using Microsoft.AspNetCore.Mvc;
using poc_mercadopago.Infrastructure.Webhooks.MercadoPago.DTOs;
using poc_mercadopago.Infrastructure.Webhooks.MercadoPago.Handlers;
using poc_mercadopago.Infrastructure.Webhooks.MercadoPago.Services;

namespace poc_mercadopago.Presentation.WebHooks
{
    /// <summary>
    /// Controller unificado para webhooks de Mercado Pago.
    ///
    /// Soporta:
    /// - Checkout Pro (type=payment, appType=checkout)
    /// - QR Dinámico (topic=merchant_order, appType=qr)
    ///
    /// Pipeline:
    /// 1. Parsear y normalizar notificación
    /// 2. Validar firma (x-signature)
    /// 3. Verificar idempotencia (evitar duplicados)
    /// 4. Rutear al handler correcto por AppType
    /// 5. Retornar 200 OK (siempre, para evitar reintentos de MP)
    /// </summary>
    [ApiController]
    [Route("webhooks/mercadopago")]
    public sealed class MercadoPagoWebhookController : ControllerBase
    {
        private readonly IEnumerable<IWebhookHandler> _handlers;
        private readonly IWebhookSignatureValidator _signatureValidator;
        private readonly IWebhookIdempotencyService _idempotencyService;
        private readonly ILogger<MercadoPagoWebhookController> _logger;

        public MercadoPagoWebhookController(
            IEnumerable<IWebhookHandler> handlers,
            IWebhookSignatureValidator signatureValidator,
            IWebhookIdempotencyService idempotencyService,
            ILogger<MercadoPagoWebhookController> logger)
        {
            _handlers = handlers;
            _signatureValidator = signatureValidator;
            _idempotencyService = idempotencyService;
            _logger = logger;
        }

        /// <summary>
        /// Endpoint único para todas las notificaciones de Mercado Pago.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Receive(
            [FromQuery] string? type,
            [FromQuery] string? topic,
            [FromQuery] long? id,
            [FromQuery(Name = "data.id")] string? dataId,
            [FromQuery] string? appType)
        {
            // 1. Parsear notificación
            var notification = ParseNotification(type, topic, id, dataId, appType);

            _logger.LogInformation(
                "Webhook recibido. Type: {Type}, AppType: {AppType}, ResourceId: {ResourceId}, NotificationId: {NotificationId}",
                notification.RawType,
                notification.AppType,
                notification.ResourceId,
                notification.NotificationId
            );

            // 2. Validar firma
            if (!ValidateSignature(notification, dataId))
            {
                _logger.LogWarning(
                    "Webhook rechazado por firma inválida. NotificationId: {NotificationId}",
                    notification.NotificationId
                );
                return Ok();
            }

            // 3. Verificar idempotencia
            if (!await _idempotencyService.TryProcessAsync(notification.NotificationId))
            {
                _logger.LogInformation(
                    "Webhook duplicado ignorado. NotificationId: {NotificationId}",
                    notification.NotificationId
                );
                return Ok();
            }

            // 4. Buscar handler por AppType
            var handler = _handlers.FirstOrDefault(h => h.AppType == notification.AppType);

            if (handler is null)
            {
                _logger.LogDebug(
                    "No hay handler para AppType={AppType}, Type={Type}. Ignorando.",
                    notification.AppType,
                    notification.RawType
                );
                return Ok();
            }

            // 5. Procesar
            var result = await handler.HandleAsync(notification);

            _logger.LogInformation(
                "Webhook procesado. NotificationId: {NotificationId}, Success: {Success}, Status: {Status}",
                result.NotificationId,
                result.Success,
                result.Status
            );

            // 6. Siempre retornar 200 OK
            return Ok();
        }

        /// <summary>
        /// Parsea los parámetros del query string en un WebhookNotification normalizado.
        /// </summary>
        private WebhookNotification ParseNotification(
            string? type,
            string? topic,
            long? id,
            string? dataId,
            string? appType)
        {
            // MP puede enviar 'type' o 'topic' según el producto
            var rawType = type ?? topic ?? "unknown";

            // El ID puede venir como 'id' o 'data.id'
            var resourceId = id ?? (long.TryParse(dataId, out var parsed) ? parsed : 0);

            // Determinar tipo normalizado
            var notificationType = rawType.ToLowerInvariant() switch
            {
                "payment" => WebhookNotificationType.Payment,
                "merchant_order" => WebhookNotificationType.MerchantOrder,
                "order" => WebhookNotificationType.MerchantOrder, // nueva API /v1/orders
                _ => WebhookNotificationType.Unknown
            };

            // Si appType no viene en la URL, inferirlo del tipo de notificación
            // Esto cubre el caso en que la notification_url no incluye ?appType=
            if (string.IsNullOrEmpty(appType))
            {
                appType = notificationType switch
                {
                    WebhookNotificationType.Payment => "checkout",
                    WebhookNotificationType.MerchantOrder => "qr",
                    _ => null
                };
            }

            // Extraer headers para validación y auditoría
            var xSignature = Request.Headers["x-signature"].FirstOrDefault();
            var xRequestId = Request.Headers["x-request-id"].FirstOrDefault();
            var sourceIp = HttpContext.Connection.RemoteIpAddress?.ToString();
            var userAgent = Request.Headers["User-Agent"].FirstOrDefault();

            return new WebhookNotification
            {
                NotificationId = $"{rawType}_{resourceId}",
                Type = notificationType,
                RawType = rawType,
                ResourceId = resourceId,
                AppType = appType,
                ReceivedAt = DateTimeOffset.UtcNow,
                Signature = xSignature,
                RequestId = xRequestId,
                SourceIp = sourceIp,
                UserAgent = userAgent,
                QueryString = Request.QueryString.Value
            };
        }

        /// <summary>
        /// Valida la firma del webhook usando el AppType para seleccionar el secret correcto.
        /// </summary>
        private bool ValidateSignature(WebhookNotification notification, string? dataId)
        {
            var isValid = _signatureValidator.Validate(
                notification.AppType,
                notification.Signature,
                notification.RequestId,
                dataId
            );

            notification.IsSignatureValid = isValid;
            return isValid;
        }
    }
}
