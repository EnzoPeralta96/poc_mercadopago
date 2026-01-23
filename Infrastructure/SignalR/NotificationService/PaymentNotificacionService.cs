using MercadoPago.Resource.Payment;
using Microsoft.AspNetCore.SignalR;
using poc_mercadopago.Infrastructure.SignalR.DTO;
using poc_mercadopago.Infrastructure.SignalR.Hub;

namespace poc_mercadopago.Infrastructure.SignalR.NotificationService
{
    /// <summary>
    /// Servicio que envía notificaciones de pago a clientes conectados vía SignalR.
    ///
    /// Responsabilidades:
    /// - Construir el mensaje de notificación según el estado del pago
    /// - Enviar la notificación al grupo correcto (por orderId)
    ///
    /// Uso:
    /// Este servicio es llamado desde el WebhookController después de procesar
    /// un webhook de Mercado Pago. Notifica al cliente que está esperando
    /// con el QR visible para que pueda redirigirlo.
    ///
    /// Flujo completo:
    /// 1. Usuario ve QR -> Cliente se conecta a SignalR y se une al grupo
    /// 2. Usuario paga con la app de MP
    /// 3. MP envía webhook al servidor
    /// 4. Webhook procesa y llama a este servicio
    /// 5. Este servicio envía notificación al grupo
    /// 6. Cliente recibe notificación y redirige al usuario
    /// </summary>
    public class PaymentNotificationService : IPaymentNotificationService
    {
        private readonly IHubContext<PaymentNotificationHub, IPaymentNotificationClient> _hubContext;
        private readonly ILogger<PaymentNotificationService> _logger;

        public PaymentNotificationService(IHubContext<PaymentNotificationHub, IPaymentNotificationClient> hubContext, ILogger<PaymentNotificationService> logger)
        {
            _hubContext = hubContext;
            _logger = logger;
        }

        /// <summary>
        /// Genera un mensaje amigable para mostrar al usuario según el estado del pago.
        ///
        /// IMPORTANTE: Para merchant orders (QR), el estado de pago exitoso es "closed",
        /// no "approved". Por eso incluimos "closed" en los estados exitosos.
        ///
        /// Estados de merchant_order:
        /// - "opened": QR generado, esperando pago
        /// - "closed": Pago completado exitosamente (PaidAmount >= TotalAmount)
        /// - "expired": Orden expirada sin pago
        ///
        /// Estados de payment (Checkout Pro):
        /// - "approved": Pago aprobado
        /// - "pending": Pago pendiente
        /// - "rejected": Pago rechazado
        /// </summary>
        private static string GenerateMessage(string? status)
        {
            return status?.ToLower() switch
            {
                // Estados exitosos: "closed" es para QR, "approved"/"paid" para otros flujos
                "approved" or "paid" or "closed" => "¡Pago aprobado exitosamente!",
                "rejected" => "El pago fue rechazado",
                // "opened" es el estado inicial de merchant_order (esperando pago)
                "pending" or "opened" => "El pago está siendo procesado",
                "cancelled" => "El pago fue cancelado",
                _ => "El estado del pago ha sido actualizado"
            };
        }

        /// <summary>
        /// Envía una notificación de pago completado a todos los clientes
        /// suscritos al grupo de la orden.
        ///
        /// El cliente JavaScript debe tener un handler para "PaymentCompleted":
        /// connection.on("PaymentCompleted", (notification) => { ... });
        ///
        /// El notification contiene:
        /// - orderId: ID de la orden local
        /// - status: Estado del pago ("closed" = exitoso para QR)
        /// - paymentId: ID del pago en MP (para referencia)
        /// - timestamp: Fecha/hora de la notificación
        /// - message: Mensaje amigable para mostrar al usuario
        /// </summary>
        /// <param name="orderId">ID de la orden local (usado para el nombre del grupo)</param>
        /// <param name="status">Estado del pago desde MP ("closed", "opened", etc.)</param>
        /// <param name="payment_id">ID del pago en MP (opcional, puede ser null)</param>
        public async Task NotifyPaymentCompletdedAsync(string orderId, string status, long? payment_id)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(orderId))
                {
                    _logger.LogWarning("El OrderId proporcionado es nulo o vacío. No se enviará la notificación.");
                    return;
                }

                // El nombre del grupo debe coincidir con el usado en JoinOrderGroup
                var groupName = $"order_{orderId}";

                // Construir la notificación con todos los datos necesarios para el cliente
                var notification = new PaymentCompletedNotification
                {
                    OrderId = orderId,
                    Status = status ?? "unknown",  // El cliente usa esto para decidir a dónde redirigir
                    PaymentId = payment_id,
                    Timestamp = DateTimeOffset.UtcNow,
                    Message = GenerateMessage(status)  // Mensaje amigable para el toast
                };

                // Enviar a todos los clientes del grupo (los que están viendo el QR de esta orden)
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