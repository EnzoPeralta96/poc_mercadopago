using Microsoft.AspNetCore.SignalR;
using poc_mercadopago.Infrastructure.SignalR.DTO;

namespace poc_mercadopago.Infrastructure.SignalR.Hub
{
    /// <summary>
    /// Interfaz tipada para los métodos que el servidor puede invocar en el cliente.
    ///
    /// SignalR usa esta interfaz para generar proxies tipados.
    /// Cada método aquí corresponde a un evento que el cliente JavaScript debe manejar.
    ///
    /// Uso en el cliente (JavaScript):
    /// connection.on("PaymentCompleted", (notification) => { ... });
    /// </summary>
    public interface IPaymentNotificationClient
    {
        /// <summary>
        /// Notifica al cliente que un pago ha sido procesado.
        /// El servidor llama esto cuando llega un webhook de MP confirmando el pago.
        /// </summary>
        /// <param name="notification">Datos del pago incluyendo status y mensaje</param>
        Task PaymentCompleted(PaymentCompletedNotification notification);
    }

    /// <summary>
    /// Hub de SignalR para notificaciones de pago en tiempo real.
    ///
    /// Arquitectura:
    /// - Cada cliente que muestra un QR se conecta a este hub
    /// - El cliente se une a un grupo específico de su orden (JoinOrderGroup)
    /// - Cuando llega un webhook de pago, el servicio envía notificación al grupo
    /// - Solo los clientes de esa orden reciben la notificación
    ///
    /// Grupos:
    /// - Nombre del grupo: "order_{orderId}"
    /// - Un grupo por cada orden activa
    /// - Permite notificar solo a los clientes relevantes
    ///
    /// Flujo:
    /// 1. Cliente se conecta al hub (/hubs/payment-notification)
    /// 2. Cliente llama JoinOrderGroup(orderId)
    /// 3. Webhook llega y PaymentNotificationService notifica al grupo
    /// 4. Cliente recibe PaymentCompleted y redirige al usuario
    ///
    /// Configuración en Program.cs:
    /// app.MapHub<PaymentNotificationHub>("/hubs/payment-notification");
    /// </summary>
    public class PaymentNotificationHub : Hub<IPaymentNotificationClient>
    {
        private readonly ILogger<PaymentNotificationHub> _logger;

        public PaymentNotificationHub(ILogger<PaymentNotificationHub> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Permite al cliente unirse al grupo de una orden específica.
        ///
        /// El cliente JavaScript llama esto después de conectarse:
        /// await connection.invoke("JoinOrderGroup", orderId);
        ///
        /// Esto permite que cuando llegue un webhook para esa orden,
        /// solo este cliente (y otros viendo el mismo QR) reciban la notificación.
        /// </summary>
        /// <param name="orderId">ID de la orden local que el cliente está monitoreando</param>
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

            // El nombre del grupo incluye prefijo "order_" para evitar colisiones
            // y facilitar debugging en los logs
            await Groups.AddToGroupAsync(Context.ConnectionId, $"order_{orderId}");

            _logger.LogInformation(
                "Connection {ConnectionId} joined group order_{OrderId}",
                Context.ConnectionId,
                orderId
            );
        }

    }
}