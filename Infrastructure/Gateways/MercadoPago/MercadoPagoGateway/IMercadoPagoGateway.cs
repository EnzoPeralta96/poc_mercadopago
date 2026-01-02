using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using poc_mercadopago.Infrastructure.Gateways.MercadoPago.DTO;

namespace poc_mercadopago.Infrastructure.Gateways.MercadoPago
{
    public interface IMercadoPagoGateway
    {
        Task<string> CreatePreferenceAsync(CreatePreferenceRequest request, CancellationToken cancellationToken = default);
        Task<PaymentDetailsDTO> GetPaymentAsync(long paymentId, CancellationToken cancellationToken = default);
    }
}