using Microsoft.AspNetCore.SignalR;
using poc_mercadopago.Infrastructure.SignalR.DTO;

namespace poc_mercadopago.Infrastructure.SignalR.Hub
{
    public interface IPaymentNotificationClient
    {
        Task PaymentCompleted(PaymentCompletedNotification notification);
    }

    public class PaymentNotificationHub : Hub<IPaymentNotificationClient>
    {
        private readonly ILogger<PaymentNotificationHub> _logger;

        public PaymentNotificationHub(ILogger<PaymentNotificationHub> logger)
        {
            _logger = logger;
        }

        public async Task JoinOrderGroup(string orderId)
        {
            if (string.IsNullOrWhiteSpace(orderId))
            {
                _logger.LogWarning(
                    "La conexión {ConnectionId} intentó unirse al grupo con un orderId no válido",
                    Context.ConnectionId
                );
                return;
            }

            await Groups.AddToGroupAsync(Context.ConnectionId, $"order_{orderId}");
            _logger.LogInformation(
                "Connection {ConnectionId} joined group {OrderId}",
                Context.ConnectionId,
                orderId
            );
        }

    }
}