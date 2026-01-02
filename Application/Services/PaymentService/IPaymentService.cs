using poc_mercadopago.Application.DTOs.StartCheckoutDTO;
namespace poc_mercadopago.Application.Services.PaymentService
{
    public interface IPaymentService
    {
        Task<StartCheckoutRespone> StartCheckoutAsync(StartCheckoutRequest request, CancellationToken cancellationToken = default);
        Task<PaymentResultDTO> GetPaymentResultAsync(long paymentId, CancellationToken cancellationToken = default);
    }
}

