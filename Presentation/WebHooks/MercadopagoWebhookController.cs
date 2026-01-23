using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using poc_mercadopago.Application.Services.PaymentService;
using poc_mercadopago.Infrastructure.SignalR.NotificationService;

namespace poc_mercadopago.Presentation.WebHooks
{
    /// <summary>
    /// Controlador que recibe las notificaciones (webhooks) de Mercado Pago.
    ///
    /// MP envía webhooks cuando ocurren eventos importantes:
    /// - Checkout Pro: type="payment" con el payment_id
    /// - QR Dinámico: topic="merchant_order" con el merchant_order_id
    ///
    /// IMPORTANTE: Los webhooks deben ser accesibles desde internet.
    /// En desarrollo, usar ngrok para exponer el servidor local.
    ///
    /// Seguridad (TODO):
    /// - Validar que la request viene realmente de MP
    /// - Implementar firma/verificación de webhooks
    /// - Rate limiting para prevenir ataques
    /// </summary>
    [ApiController]
    public sealed class MercadopagoWebhookController : ControllerBase
    {
        private readonly ILogger<MercadopagoWebhookController> _logger;
        private readonly IPaymentService _paymentService;
        private readonly IPaymentNotificationService _signalRNotificacionService;

        public MercadopagoWebhookController(IPaymentService paymentService, ILogger<MercadopagoWebhookController> logger, IPaymentNotificationService signalRNotificacionService)
        {
            _logger = logger;
            _paymentService = paymentService;
            _signalRNotificacionService = signalRNotificacionService;
        }

        /*
            TODO: Mejoras de seguridad pendientes
            - Validar firma del webhook de MP
            - Implementar rate limiting
            - Sanitizar logs para prevenir inyección de logs
            - Implementar idempotencia (evitar procesar el mismo webhook dos veces)
        */

        [HttpPost("webhooks/mercadopago")]
        public async Task<IActionResult> ReceiveCheckoutProWebHook([FromQuery] string? type, [FromQuery] long? id, [FromQuery(Name = "data.id")] long? dataId)
        {
            _logger.LogInformation("Webhook recibido - type={Type}, id={Id}, data.id={DataId}", type, id, dataId);

            // Mercado Pago puede enviar el ID de pago como 'id' o 'data.id'
            var paymentId = id ?? dataId;

            //Ver que otros tipos de webhooks manda mercadopago.
            if (type != "payment" || !paymentId.HasValue)
            {
                _logger.LogWarning("Webhook ignorado - type={Type}, paymentId={PaymentId}", type, paymentId);
                return Ok();
            }

            var paymentResult = await _paymentService.GetPaymentResultAsync(paymentId.Value);

            if (paymentResult is null)
            {
                _logger.LogError("Error procesando webhook para payment_id={PaymentId}", paymentId);
            }

            _logger.LogInformation(
                  "Pago procesado exitosamente: PaymentId={PaymentId} OrderId={OrderId} Status={Status}",
                  paymentResult.PaymentId,
                  paymentResult.OrderId,
                  paymentResult.Status
              );

            return Ok();
        }

        /// <summary>
        /// Endpoint que recibe las notificaciones de Mercado Pago para pagos con QR dinámico.
        ///
        /// URL configurada en el campo notification_url al crear la orden QR.
        /// Ejemplo: https://tu-dominio.ngrok.dev/webhooks/mercadopago/qr
        ///
        /// MP envía una notificación cuando:
        /// - Se crea la merchant order (QR generado)
        /// - El usuario escanea y paga el QR
        /// - La orden expira
        ///
        /// Parámetros que envía MP (en query string):
        /// - topic: Tipo de notificación ("merchant_order" para QR)
        /// - type: Alternativa a topic (algunos webhooks usan type en lugar de topic)
        /// - id: merchant_order_id (ID de MP, NO es nuestro OrderId)
        /// - data.id: Formato alternativo del ID
        ///
        /// IMPORTANTE: El merchant_order_id no es nuestro OrderId.
        /// Debemos consultar a MP para obtener el external_reference (nuestro OrderId).
        ///
        /// Flujo:
        /// 1. Recibir webhook con merchant_order_id
        /// 2. Llamar a ProcessMerchantOrderWebhookAsync que:
        ///    - Consulta GET /merchant_orders/{id} a MP
        ///    - Extrae external_reference (nuestro OrderId)
        ///    - Actualiza el estado de la orden local
        /// 3. Notificar al cliente vía SignalR
        /// 4. Retornar 200 OK (siempre, para evitar reintentos de MP)
        /// </summary>
        [HttpPost("webhooks/mercadopago/qr")]
        public async Task<IActionResult> ReceiveQrWebhook(
            [FromQuery] string? topic,           // Tipo de notificación (debería ser "merchant_order")
            [FromQuery] string? type,            // Alternativa a topic
            [FromQuery] long? id,                // merchant_order_id de MP
            [FromQuery(Name = "data.id")] long? dataId  // Formato alternativo del ID
        )
        {
            _logger.LogInformation(
              "Webhook QR recibido - Topic: {Topic}, Type: {Type}, Id: {Id}, DataId: {DataId}",
              topic,
              type,
              id,
              dataId
          );

            // MP puede enviar el tipo como "topic" o "type" según el webhook
            var notificationType = topic ?? type;

            // El ID puede venir como "id" o "data.id"
            var merchantOrderId = id ?? dataId;

            // Solo procesamos notificaciones de tipo "merchant_order" con un ID válido
            // Otros tipos (como "payment") no aplican para QR dinámico
            if (notificationType != "merchant_order" || !merchantOrderId.HasValue)
            {
                _logger.LogWarning(
                  "Webhook QR ignorado - Topic/Type: {NotificationType}, MerchantOrderId: {MerchantOrderId}",
                  notificationType,
                  merchantOrderId
              );
                return Ok();  // Siempre retornar 200 para evitar reintentos
            }

            // Procesar el webhook: consultar a MP, extraer external_reference, actualizar orden
            // NOTA: merchantOrderId es el ID de MP, no nuestro OrderId
            var qrPaymentStatus = await _paymentService.ProcessMerchantOrderWebhookAsync(merchantOrderId.Value);

            if (qrPaymentStatus is null)
            {
                _logger.LogError(
                    "No se pudo obtener merchant order {MerchantOrderId} desde MP",
                    merchantOrderId
                );
                return Ok();  // Retornar 200 para evitar reintentos infinitos
            }

            _logger.LogInformation(
              "Merchant order procesado. MerchantOrderId: {MerchantOrderId}, OrderId: {OrderId}, Status: {Status}",
              merchantOrderId,
              qrPaymentStatus.OrderId,
              qrPaymentStatus.Status
          );

            // Notificar al cliente vía SignalR para que actualice la UI
            // El status "closed" indica que el pago fue completado exitosamente
            await _signalRNotificacionService.NotifyPaymentCompletdedAsync(
                qrPaymentStatus.OrderId,    // Nuestro OrderId (para el grupo SignalR)
                qrPaymentStatus.Status,     // Estado de MP ("closed" = pagado)
                qrPaymentStatus.PaymentId); // ID del pago en MP

            return Ok();
        }
    }
}