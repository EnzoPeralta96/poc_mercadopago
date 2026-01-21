using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using poc_mercadopago.Application.Services.PaymentService;
using poc_mercadopago.Infrastructure.SignalR.NotificationService;

namespace poc_mercadopago.Presentation.WebHooks
{
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
            Investigar mas a fondo los webhooks de mp.
            Hacer los Test.
            Investigar acerca de la seguridad y que solo MP pueda mandar los post.
            Ver solucion y/o tener la medicina necesario en caso de que ocurra un problema
            Prevenir inyeciones -  Prevenir Que exploten los log.
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

        [HttpPost("webhooks/mercadopago/qr")]
        public async Task<IActionResult> ReceiveQrWebhook(
            [FromQuery] string? topic,
            [FromQuery] string? type,
            [FromQuery] long? id,
            [FromQuery(Name = "data.id")] long? dataId
        )
        {
            _logger.LogInformation(
              "Webhook QR recibido - Topic: {Topic}, Type: {Type}, Id: {Id}, DataId: {DataId}",
              topic,
              type,
              id,
              dataId
          );

            var notificationType = topic ?? type;

            var merchantOrderId = id ?? dataId;

            if (notificationType != "merchant_order" || !merchantOrderId.HasValue)
            {
                _logger.LogWarning(
                  "Webhook QR ignorado - Topic/Type: {NotificationType}, MerchantOrderId: {MerchantOrderId}",
                  notificationType,
                  merchantOrderId
              );
                return Ok();
            }

            // Usar el nuevo método que consulta a MercadoPago y extrae el external_reference
            var qrPaymentStatus = await _paymentService.ProcessMerchantOrderWebhookAsync(merchantOrderId.Value);

            if (qrPaymentStatus is null)
            {
                _logger.LogError(
                    "No se pudo obtener merchant order {MerchantOrderId} desde MP",
                    merchantOrderId
                );
                return Ok();
            }

            _logger.LogInformation(
              "Merchant order procesado. MerchantOrderId: {MerchantOrderId}, OrderId: {OrderId}, Status: {Status}",
              merchantOrderId,
              qrPaymentStatus.OrderId,
              qrPaymentStatus.Status
          );

            await _signalRNotificacionService.NotifyPaymentCompletdedAsync(
                qrPaymentStatus.OrderId,
                qrPaymentStatus.Status,
                qrPaymentStatus.PaymentId);

            return Ok();
        }
    }
}