using MercadoPago.Resource.Payment;
using Microsoft.AspNetCore.SignalR;
using poc_mercadopago.Infrastructure.SignalR.DTO;
using poc_mercadopago.Infrastructure.SignalR.Hub;

namespace poc_mercadopago.Infrastructure.SignalR.NotificationService
{
    public class PaymentNotificationService : IPaymentNotificationService
    {
        private readonly IHubContext<PaymentNotificationHub, IPaymentNotificationClient> _hubContext;
        private readonly ILogger<PaymentNotificationService> _logger;

        public PaymentNotificationService(IHubContext<PaymentNotificationHub, IPaymentNotificationClient> hubContext, ILogger<PaymentNotificationService> logger)
        {
            _hubContext = hubContext;
            _logger = logger;
        }

        private static string GenerateMessage(string? status)
        {
            return status?.ToLower() switch
            {
                "approved" or "paid" or "closed" => "¡Pago aprobado exitosamente!",
                "rejected" => "El pago fue rechazado",
                "pending" or "opened" => "El pago está siendo procesado",
                "cancelled" => "El pago fue cancelado",
                _ => "El estado del pago ha sido actualizado"
            };
        }

        public async Task NotifyPaymentCompletdedAsync(string orderId, string status, long? payment_id)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(orderId))
                {
                    _logger.LogWarning("El OrderId proporcionado es nulo o vacío. No se enviará la notificación.");
                    return;
                }

                var groupName = $"order_{orderId}";

                var notification = new PaymentCompletedNotification
                {
                    OrderId = orderId,
                    Status = status ?? "unknown",
                    PaymentId = payment_id,
                    Timestamp = DateTimeOffset.UtcNow,
                    Message = GenerateMessage(status)
                };

                await _hubContext.Clients.Group(groupName).PaymentCompleted(notification);

                _logger.LogInformation(
                       "Notificación de pago enviada al grupo {GroupName}: Status={Status}, PaymentId={PaymentId}",
                       groupName,
                       status,
                       payment_id
                   );
            }
            catch (Exception ex)
            {
                _logger.LogError(
                   ex,
                   "Error al enviar notificación de pago para orden {OrderId}",
                   orderId
               );

            }

        }
    }
}