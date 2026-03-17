using System.Diagnostics;
using poc_mercadopago.Application.DTOs.StartQrDTO;
using poc_mercadopago.Application.Services.PaymentService;
using poc_mercadopago.Infrastructure.SignalR.NotificationService;
using poc_mercadopago.Infrastructure.Webhooks.MercadoPago.DTOs;

namespace poc_mercadopago.Infrastructure.Webhooks.MercadoPago.Handlers
{
    public class MerchantOrderWebhookHandler : IWebhookHandler
    {
        private readonly IPaymentService _paymentService;
        private readonly ILogger<MerchantOrderWebhookHandler> _logger;
        private readonly IPaymentNotificationService _signalRService;

        public MerchantOrderWebhookHandler(
            IPaymentService paymentService,
            ILogger<MerchantOrderWebhookHandler> logger,
            IPaymentNotificationService signalRService
        )
        {
            _paymentService = paymentService;
            _logger = logger;
            _signalRService = signalRService;
        }

        public string AppType => "qr";

        public async Task<WebhookProcessingResult> HandleAsync(WebhookNotification notification)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                // La vieja API QR (/instore/orders/qr/...) envía type=payment con payment IDs.
                // La nueva API QR (/v1/orders) envía type=order con merchant order IDs.
                if (notification.Type == WebhookNotificationType.Payment)
                {
                    return await HandlePaymentNotificationAsync(notification, stopwatch);
                }

                return await HandleMerchantOrderNotificationAsync(notification, stopwatch);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger.LogError(
                    ex,
                    "Error procesando webhook QR. ResourceId: {ResourceId}",
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

        private async Task<WebhookProcessingResult> HandlePaymentNotificationAsync(WebhookNotification notification, Stopwatch stopwatch)
        {
            var paymentResult = await _paymentService.GetPaymentResultAsync(notification.ResourceId);

            if (paymentResult is null)
            {
                stopwatch.Stop();
                _logger.LogWarning("No se pudo obtener pago QR {PaymentId}", notification.ResourceId);
                return WebhookProcessingResult.Failed(
                    notification.NotificationId,
                    "No se pudo obtener información del pago desde MP",
                    processingTime: stopwatch.Elapsed
                );
            }

            var signalRSent = await SendSignalRNotificationAsync(paymentResult.OrderId, paymentResult.Status, paymentResult.PaymentId);

            stopwatch.Stop();

            _logger.LogInformation(
                "Pago QR procesado. PaymentId: {PaymentId}, OrderId: {OrderId}, Status: {Status}, SignalR: {SignalR}",
                paymentResult.PaymentId,
                paymentResult.OrderId,
                paymentResult.Status,
                signalRSent
            );

            return WebhookProcessingResult.Successful(
                notification.NotificationId,
                orderId: paymentResult.OrderId,
                newStatus: paymentResult.Status,
                signalRSent: signalRSent,
                processingTime: stopwatch.Elapsed
            );
        }

        private async Task<WebhookProcessingResult> HandleMerchantOrderNotificationAsync(WebhookNotification notification, Stopwatch stopwatch)
        {
            var qrPaymentStatus = await _paymentService.ProcessMerchantOrderWebhookAsync(notification.ResourceId);

            if (qrPaymentStatus is null)
            {
                stopwatch.Stop();
                _logger.LogWarning("No se pudo obtener estado del MerchantOrder {MerchantOrderId}", notification.ResourceId);
                return WebhookProcessingResult.Failed(
                    notification.NotificationId,
                    "No se pudo obtener información del merchant_order desde MP",
                    processingTime: stopwatch.Elapsed
                );
            }

            var signalRSent = await SendSignalRNotificationAsync(qrPaymentStatus.OrderId, qrPaymentStatus.Status, qrPaymentStatus.PaymentId);

            stopwatch.Stop();

            _logger.LogInformation(
                "MerchantOrder procesado. OrderId: {OrderId}, Status: {Status}, SignalR: {SignalR}",
                qrPaymentStatus.OrderId,
                qrPaymentStatus.Status,
                signalRSent
            );

            return WebhookProcessingResult.Successful(
                notification.NotificationId,
                orderId: qrPaymentStatus.OrderId,
                newStatus: qrPaymentStatus.Status,
                signalRSent: signalRSent,
                processingTime: stopwatch.Elapsed
            );
        }

        private async Task<bool> SendSignalRNotificationAsync(string? orderId, string? status, long? paymentId)
        {
            try
            {
                await _signalRService.NotifyPaymentCompletdedAsync(orderId, status, paymentId);
                _logger.LogInformation("SignalR enviado para orden {OrderId}", orderId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al enviar notificación SignalR para orden {OrderId}", orderId);
                return false;
            }
        }
    }
}
