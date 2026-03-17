using System.Diagnostics;
using poc_mercadopago.Application.Services.PaymentService;
using poc_mercadopago.Helpers;
using poc_mercadopago.Infrastructure.Webhooks.MercadoPago.DTOs;

namespace poc_mercadopago.Infrastructure.Webhooks.MercadoPago.Handlers
{
    public class PaymentWebhookHandler : IWebhookHandler
    {
        private readonly IPaymentService _paymentService;
        private readonly ILogger<PaymentWebhookHandler> _logger;

        public PaymentWebhookHandler(IPaymentService paymentService, ILogger<PaymentWebhookHandler> logger)
        {
            _paymentService = paymentService;
            _logger = logger;
        }

        public string AppType => "checkout";
        public async Task<WebhookProcessingResult> HandleAsync(WebhookNotification notification)
        {
            //mide el tiempo que toma procesar la notificación para monitoreo y debugging
            var stopwatch = Stopwatch.StartNew();
            try
            {
                _logger.LogInformation("Procesando webhook Payment. PaymentId: {PaymentId}", notification.ResourceId);

                // Lógica de negocio para procesar el pago
                var paymentResult = await _paymentService.GetPaymentResultAsync(notification.ResourceId);

                stopwatch.Stop();

                if (paymentResult is null)
                {
                    _logger.LogWarning(
                      "No se pudo obtener resultado del pago {PaymentId}",
                      notification.ResourceId
                    );

                    return WebhookProcessingResult.Failed(
                        notification.NotificationId,
                        "No se pudo obtener información del pago desde MP",
                        processingTime: stopwatch.Elapsed
                    );
                }

                _logger.LogInformation(
                    "Payment procesado. PaymentId: {PaymentId}, OrderId: {OrderId}, Status: {Status}",
                    paymentResult.PaymentId,
                    LogSanitizer.Sanitize(paymentResult.OrderId),
                    LogSanitizer.Sanitize(paymentResult.Status)
                );

                /*
                    ¿Por qué signalRSent: false?
                        Checkout Pro usa redirección.
                        El usuario ya está en la página de resultado cuando llega el webhook.
                        No necesita notificación en tiempo real.
                */
                return WebhookProcessingResult.Successful(
                    notification.NotificationId,
                    orderId: paymentResult.OrderId,
                    newStatus: paymentResult.Status,
                    signalRSent: false,
                    processingTime: stopwatch.Elapsed
                );

            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error al procesar webhook de pago - ResourceId: {ResourceId}",
                    notification.ResourceId
                );

                return WebhookProcessingResult.Failed(
                    notification.NotificationId,
                    ex.Message,
                    exception: ex,
                    processingTime: stopwatch.Elapsed
                );
            }

        }
    }
}
