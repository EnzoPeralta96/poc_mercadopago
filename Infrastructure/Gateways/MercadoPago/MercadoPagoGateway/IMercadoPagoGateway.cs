using poc_mercadopago.Infrastructure.Gateways.MercadoPago.DTO;
namespace poc_mercadopago.Infrastructure.Gateways.MercadoPago.MercadoPagoGateway
{
    public interface IMercadoPagoGateway
    {
        Task<string> CreatePreferenceAsync(CreatePreferenceRequest request, CancellationToken cancellationToken = default);
        Task<PaymentDetailsDTO> GetPaymentAsync(long paymentId, CancellationToken cancellationToken = default);
    }

}