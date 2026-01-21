using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using poc_mercadopago.Infrastructure.Gateways.MercadoPago.Configuration;
using poc_mercadopago.Infrastructure.Gateways.MercadoPago.DTO.QrDTO;

namespace poc_mercadopago.Infrastructure.Gateways.MercadoPago.MercadoPagoQRGateway
{
    public class MercadoPagoQRGateway : IMercadoPagoQRGateway
    {
        private readonly MercadoPagoQrOptions _mercadoPagoQrOptions;
        private readonly ILogger<MercadoPagoQRGateway> _logger;
        private readonly IHttpClientFactory _httpClientFactory;

        private const string MercadoPagoApiBaseUrl = "https://api.mercadopago.com";
        public MercadoPagoQRGateway(ILogger<MercadoPagoQRGateway> logger, IOptions<MercadoPagoQrOptions> options, IHttpClientFactory httpClientFactory)
        {
            _logger = logger;
            _mercadoPagoQrOptions = options.Value;
            _httpClientFactory = httpClientFactory;
        }


        private HttpClient GetHttpClient()
        {
            var httpClient = _httpClientFactory.CreateClient();

            // Configurar headers
            httpClient.DefaultRequestHeaders.Clear();
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _mercadoPagoQrOptions.AccessToken);
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            return httpClient;
        }

        private string GetUrlPostQR()
        {
            var url = $"{MercadoPagoApiBaseUrl}/instore/orders/qr/seller/collectors/{_mercadoPagoQrOptions.UserId}/pos/{_mercadoPagoQrOptions.ExternalPosId}/qrs";
            return url;
        }

        private string GetUrlGetMerchantOrder(string id)
        {
            var url = $"{MercadoPagoApiBaseUrl}/merchant_orders/{id}";
            return url;
        }

        private StringContent TransformRequestToJson(QrOrderRequest request)
        {
            var jsonContent = JsonSerializer.Serialize(request, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            });

            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
            return content;
        }
        public async Task<QrOrderResponse> CreateQrOrderAsync(QrOrderRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                // Construir URL del endpoint
                string url = GetUrlPostQR();

                // Crear HttpClient configurado
                HttpClient httpClient = GetHttpClient();

                // Serializar request a JSON
                StringContent content = TransformRequestToJson(request);

                // Enviar solicitud POST
                var response = await httpClient.PostAsync(url, content, cancellationToken);

                //Leer respuesta
                var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError(
                        "Error al crear orden QR en Mercado Pago. StatusCode: {StatusCode}, Response: {Response}",
                        response.StatusCode,
                        responseBody
                    );

                    throw new HttpRequestException($"Error al crear orden QR en Mercado Pago. StatusCode: {response.StatusCode}");
                }

                // Deserializar respuesta
                var qrOrderResponse = JsonSerializer.Deserialize<QrOrderResponse>(responseBody, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (qrOrderResponse == null || string.IsNullOrEmpty(qrOrderResponse.QrData))
                {
                    _logger.LogError("Error al deserializar la respuesta de la orden QR de Mercado Pago. Response: {Response}", responseBody);
                }

                return qrOrderResponse;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear orden QR para OrderId {OrderId}", request.ExternalReference);
                throw;
            }
        }


        public async Task<MerchantOrderStatusResponse> GetMerchantOrderStatusAsync(string inStoreOrderId, CancellationToken cancellationToken = default)
        {
            try
            {
                //Construir la URL del endpoint
                var url = GetUrlGetMerchantOrder(inStoreOrderId);

                //Crear HttpClient configurado
                var httpClient = GetHttpClient();

                //Enviar solicitud GET
                var response = await httpClient.GetAsync(url, cancellationToken);

                var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError(
                       "Error al consultar merchant order. StatusCode: {StatusCode}, Response: {Response}",
                        response.StatusCode,
                        responseBody
                    );

                    throw new HttpRequestException($"Error al obtener el estado de la orden en Mercado Pago. StatusCode: {response.StatusCode}");
                }

                // Deserializar respuesta
                var merchantOrderStatusResponse = JsonSerializer.Deserialize<MerchantOrderStatusResponse>(responseBody, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                
                if (merchantOrderStatusResponse == null)
                {
                    _logger.LogError("Error al deserializar la respuesta del estado de la merchant order de Mercado Pago. Response: {Response}", responseBody);
                }

                return merchantOrderStatusResponse;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al consultar merchant order {InStoreOrderId}", inStoreOrderId);
                throw;
            }
        }


    }
}