using poc_mercadopago.Infrastructure.Gateways.MercadoPago.DTO.QrDTO;

namespace poc_mercadopago.Infrastructure.Gateways.MercadoPago.MercadoPagoQRGateway
{
    public interface IMercadoPagoQRGateway
    {
        /// <summary>
        /// Crea una orden QR dinámica en Mercado Pago.
        /// Endpoint: POST /instore/orders/qr/seller/collectors/{user_id}/pos/{external_pos_id}/qrs
        /// </summary>
        /// <param name="request">Datos de la orden QR</param>
        /// <param name="cancellationToken">Token de cancelación</param>
        /// <returns>Response con qr_data e in_store_order_id</returns>
        Task<QrOrderResponse> CreateQrOrderAsync(QrOrderRequest request, CancellationToken cancellationToken = default);

        /// <summary>
        /// Consulta el estado de una merchant order (orden QR).
        /// Endpoint: GET /merchant_orders/{id}
        /// </summary>
        /// <param name="inStoreOrderId">ID de la in-store order (UUID string)</param>
        /// <param name="cancellationToken">Token de cancelación</param>
        /// <returns>Estado actual de la orden</returns>
        Task<MerchantOrderStatusResponse> GetMerchantOrderStatusAsync(string inStoreOrderId, CancellationToken cancellationToken = default);
    }
}