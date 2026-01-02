using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using poc_mercadopago.Application.Services.PaymentService;

namespace poc_mercadopago.Presentation.WebHooks
{
    [ApiController]
    public sealed class MercadopagoWebhookController : ControllerBase
    {
        private readonly IPaymentService _paymentService;
        private readonly ILogger<MercadopagoWebhookController> _logger;
        public MercadopagoWebhookController(IPaymentService paymentService, ILogger<MercadopagoWebhookController> logger)
        {
            _paymentService = paymentService;
            _logger = logger;
        }

        [HttpPost("webhooks/mercadopago")]
        public async Task<IActionResult> Receive([FromQuery] string? type, [FromQuery] long? id, [FromQuery(Name = "data.id")] long? dataId)
        {
            _logger.LogInformation("Webhook recibido - type={Type}, id={Id}, data.id={DataId}", type, id, dataId);

            // Mercado Pago puede enviar el ID de pago como 'id' o 'data.id'
            var paymentId = id ?? dataId;

            if (type != "payment" || !paymentId.HasValue)
            {
                _logger.LogWarning("Webhook ignorado - type={Type}, paymentId={PaymentId}", type, paymentId);
                return Ok();
            }

            var paymentResult = await _paymentService.GetPaymentResultAsync(paymentId.Value);

            if (paymentResult is not null)
            {
                _logger.LogInformation(
                  "Pago procesado exitosamente: PaymentId={PaymentId} OrderId={OrderId} Status={Status}",
                  paymentResult.PaymentId,
                  paymentResult.OrderId,
                  paymentResult.Status
              );
            }else
            {
                  _logger.LogError("Error procesando webhook para payment_id={PaymentId}", paymentId);
            }

            return Ok();
        }
    }
}