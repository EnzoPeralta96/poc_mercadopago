using poc_mercadopago.Application.DTOs.StartCheckoutDTO;
using poc_mercadopago.Application.DTOs.StartQrDTO;
namespace poc_mercadopago.Application.Services.PaymentService
{
    public interface IPaymentService
    {
        Task<StartCheckoutRespone> StartCheckoutAsync(StartCheckoutRequest request, CancellationToken cancellationToken = default);
        Task<PaymentResultDTO> GetPaymentResultAsync(long paymentId, CancellationToken cancellationToken = default);

        Task<StartQrResponse> StartQrAsync(StartQrRequest request, CancellationToken cancellationToken = default);
        //Task<QrPaymentStatusDTO> GetQrPaymentStatusAsync(string orderId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Procesa una notificación de merchant order desde el webhook de MercadoPago.
        /// Consulta a MercadoPago por el merchant_order_id, extrae el external_reference (OrderId local)
        /// y actualiza el estado de la orden.
        /// </summary>
        Task<QrPaymentStatusDTO> ProcessMerchantOrderWebhookAsync(long merchantOrderId, CancellationToken cancellationToken = default);

    }
}

