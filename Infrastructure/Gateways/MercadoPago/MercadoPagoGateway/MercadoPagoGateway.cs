using MercadoPago.Client.Payment;
using MercadoPago.Client.Preference;
using MercadoPago.Config;
using Microsoft.Extensions.Options;
using poc_mercadopago.Infrastructure.Configuration;
using poc_mercadopago.Infrastructure.Gateways.MercadoPago.DTO;

namespace poc_mercadopago.Infrastructure.Gateways.MercadoPago.MercadoPagoGateway
{
    public class MercadoPagoGateway : IMercadoPagoGateway
    {
        private readonly MercadoPagoOptions _mercadoPagoOptions;
        private readonly ILogger<MercadoPagoGateway> _logger;

        public MercadoPagoGateway(ILogger<MercadoPagoGateway> logger, IOptions<MercadoPagoOptions> options)
        {
            _logger = logger;
            _mercadoPagoOptions = options.Value;
            MercadoPagoConfig.AccessToken = _mercadoPagoOptions.AccessToken;
        }

        public async Task<string> CreatePreferenceAsync(CreatePreferenceRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                var preferenceRequest = new PreferenceRequest
                {
                    ExternalReference = request.OrderId,
                    Items = request.Items.Select(item => new PreferenceItemRequest
                    {
                        Title = item.Title,
                        Quantity = item.Quantity,
                        UnitPrice = item.UnitPrice,
                        CurrencyId = item.CurrencyId
                    }).ToList(),

                    BackUrls = new PreferenceBackUrlsRequest
                    {
                        Success = $"{_mercadoPagoOptions.BaseUrl}/checkout/return/success",
                        Failure = $"{_mercadoPagoOptions.BaseUrl}/checkout/return/failure",
                        Pending = $"{_mercadoPagoOptions.BaseUrl}/checkout/return/pending",
                    },
                    AutoReturn = "approved",
                    NotificationUrl = $"{_mercadoPagoOptions.BaseUrl}/webhooks/mercadopago"
                };

                var client = new PreferenceClient();
                var preference = await client.CreateAsync(preferenceRequest);

                _logger.LogInformation(
                    "Preferencia de MercadoPago creada: {PreferenceId} para orden {OrderId}",
                    preference.Id,
                    request.OrderId
                );
                return preference.Id;

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear la preferencia de MercadoPago para orden {OrderId}", request.OrderId);
                throw;
            }
        }

        public async Task<PaymentDetailsDTO> GetPaymentAsync(long paymentId, CancellationToken cancellationToken = default)
        {
            try
            {
                var client = new PaymentClient();
                var payment = await client.GetAsync(paymentId, cancellationToken: cancellationToken);

                return new PaymentDetailsDTO
                {
                    PaymentId = payment.Id.Value,
                    Status = payment.Status,
                    OrderId = payment.ExternalReference,
                    Amount = payment.TransactionAmount ?? 0,
                    CurrencyId = payment.CurrencyId
                };
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error al obtener detalles del pago {PaymentId}", paymentId);
                throw;
            }
        }
       
    }
}