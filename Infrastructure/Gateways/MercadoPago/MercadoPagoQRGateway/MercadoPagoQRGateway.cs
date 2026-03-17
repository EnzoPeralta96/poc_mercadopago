using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using poc_mercadopago.Infrastructure.Gateways.MercadoPago.Configuration;
using poc_mercadopago.Infrastructure.Gateways.MercadoPago.DTO.QrDTO;

namespace poc_mercadopago.Infrastructure.Gateways.MercadoPago.MercadoPagoQRGateway
{
    /// <summary>
    /// Gateway para comunicación con la API de QR Dinámico de Mercado Pago.
    ///
    /// Responsabilidades:
    /// - Crear órdenes QR dinámicas (POST /instore/orders/qr/...)
    /// - Consultar estado de merchant orders (GET /merchant_orders/{id})
    ///
    /// Este gateway usa la API de "Cobros Presenciales" de MP, diferente a Checkout Pro.
    /// Requiere tener una Sucursal (Store) y Caja (POS) creados previamente en MP.
    /// </summary>
    public class MercadoPagoQRGateway : IMercadoPagoQRGateway
    {
        private readonly MercadoPagoQrOptions _mercadoPagoQrOptions;
        private readonly ILogger<MercadoPagoQRGateway> _logger;
        private readonly IHttpClientFactory _httpClientFactory;

        /// <summary>
        /// URL base de la API de Mercado Pago.
        /// Todas las llamadas a la API usan esta URL como prefijo.
        /// </summary>
        private const string MercadoPagoApiBaseUrl = "https://api.mercadopago.com";

        public MercadoPagoQRGateway(ILogger<MercadoPagoQRGateway> logger, IOptions<MercadoPagoQrOptions> options, IHttpClientFactory httpClientFactory)
        {
            _logger = logger;
            _mercadoPagoQrOptions = options.Value;
            _httpClientFactory = httpClientFactory;
        }


        /// <summary>
        /// Crea y configura un HttpClient con los headers requeridos por la API de MP.
        ///
        /// Headers configurados:
        /// - Authorization: Bearer {AccessToken} - Token del vendedor (Test User en desarrollo)
        /// - Accept: application/json
        ///
        /// IMPORTANTE: El AccessToken debe pertenecer al mismo usuario que el UserId configurado,
        /// de lo contrario se recibirá Error 403 Forbidden.
        /// </summary>
        private HttpClient GetHttpClient()
        {
            var httpClient = _httpClientFactory.CreateClient();

            // Configurar headers de autenticación y contenido
            httpClient.DefaultRequestHeaders.Clear();
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _mercadoPagoQrOptions.AccessToken);
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            return httpClient;
        }

        /// <summary>
        /// Construye la URL del endpoint para crear órdenes QR dinámicas.
        ///
        /// Estructura de la URL:
        /// POST /instore/orders/qr/seller/collectors/{user_id}/pos/{external_pos_id}/qrs
        ///
        /// Parámetros:
        /// - user_id: ID del usuario vendedor en MP (debe coincidir con el dueño del AccessToken)
        /// - external_pos_id: ID externo del Punto de Venta (caja) creado previamente en MP
        ///
        /// Este endpoint crea un QR dinámico único para cada transacción.
        /// </summary>
        private string GetUrlPostQR()
        {
            var url = $"{MercadoPagoApiBaseUrl}/instore/orders/qr/seller/collectors/{_mercadoPagoQrOptions.UserId}/pos/{_mercadoPagoQrOptions.ExternalPosId}/qrs";
            return url;
        }

        /// <summary>
        /// Construye la URL para consultar el estado de una merchant order.
        ///
        /// Endpoint: GET /merchant_orders/{id}
        ///
        /// Esta consulta es CRÍTICA porque:
        /// 1. El webhook solo envía el merchant_order_id, no los datos del pago
        /// 2. Necesitamos el external_reference para encontrar nuestra orden local
        /// 3. El status "closed" indica que el pago fue completado
        /// </summary>
        private string GetUrlGetMerchantOrder(string id)
        {
            var url = $"{MercadoPagoApiBaseUrl}/merchant_orders/{id}";
            return url;
        }

        /// <summary>
        /// Serializa el request a JSON con las convenciones de la API de MP.
        ///
        /// Configuración:
        /// - CamelCase: Los nombres de propiedades se convierten a camelCase
        /// - WhenWritingNull: Las propiedades null no se incluyen en el JSON
        ///   (importante para no enviar "sponsor" en ambiente de pruebas)
        /// </summary>
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

        /// <summary>
        /// Crea una orden QR dinámica en Mercado Pago.
        ///
        /// Flujo:
        /// 1. Construir la URL con UserId y ExternalPosId
        /// 2. Serializar el request a JSON
        /// 3. Enviar POST a la API de MP
        /// 4. Parsear la respuesta que contiene qr_data e in_store_order_id
        ///
        /// El qr_data es un string EMV que se usa para generar la imagen QR.
        /// El cliente escanea este QR con la app de Mercado Pago para pagar.
        /// </summary>
        /// <param name="request">Datos de la orden (items, total, external_reference, etc.)</param>
        /// <param name="cancellationToken">Token de cancelación</param>
        /// <returns>Response con qr_data (string EMV) e in_store_order_id</returns>
        public async Task<QrOrderResponse> CreateQrOrderAsync(QrOrderRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                // Construir URL del endpoint con UserId y ExternalPosId
                string url = GetUrlPostQR();

                // Crear HttpClient con headers de autenticación
                HttpClient httpClient = GetHttpClient();

                // Serializar request a JSON (snake_case para la API de MP)
                StringContent content = TransformRequestToJson(request);

                // Enviar solicitud POST a la API de QR de MP
                var response = await httpClient.PostAsync(url, content, cancellationToken);

                // Leer el cuerpo de la respuesta
                var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

                // Verificar si la respuesta fue exitosa
                // Errores comunes:
                // - 403: El UserId no coincide con el dueño del AccessToken
                // - 400: Datos inválidos en el request (revisar items, total_amount)
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError(
                        "Error al crear orden QR en Mercado Pago. StatusCode: {StatusCode}, Response: {Response}",
                        response.StatusCode,
                        responseBody
                    );

                    throw new HttpRequestException($"Error al crear orden QR en Mercado Pago. StatusCode: {response.StatusCode}");
                }

                
                // Deserializar la respuesta de MP
                // La respuesta contiene:
                // - qr_data: String EMV para generar la imagen QR
                // - in_store_order_id: ID de la orden en el sistema de MP
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


        /// <summary>
        /// Consulta el estado de una merchant order en Mercado Pago.
        ///
        /// Este método es CRÍTICO para el flujo de verificación de pagos QR porque:
        /// 1. El webhook solo envía el merchant_order_id (ID de MP, no nuestro OrderId)
        /// 2. Necesitamos consultar a MP para obtener el external_reference (nuestro OrderId)
        /// 3. El campo "status" nos dice si el pago fue completado ("closed")
        /// 4. El campo "payments" contiene el payment_id si hay pagos
        ///
        /// Estados posibles de merchant_order:
        /// - "opened": Esperando pago (QR generado pero no pagado)
        /// - "closed": Pagado completamente (PaidAmount >= TotalAmount)
        /// - "expired": Expirada sin pago
        /// </summary>
        /// <param name="inStoreOrderId">merchant_order_id recibido en el webhook</param>
        /// <param name="cancellationToken">Token de cancelación</param>
        /// <returns>Estado completo de la merchant order incluyendo external_reference</returns>
        public async Task<MerchantOrderStatusResponse> GetMerchantOrderStatusAsync(string inStoreOrderId, CancellationToken cancellationToken = default)
        {
            try
            {
                // Construir la URL: GET /merchant_orders/{id}
                var url = GetUrlGetMerchantOrder(inStoreOrderId);

                // Crear HttpClient con headers de autenticación
                var httpClient = GetHttpClient();

                // Enviar solicitud GET a la API de MP
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
                // Campos importantes:
                // - external_reference: Nuestro OrderId local
                // - status: "opened" | "closed" | "expired"
                // - paid_amount: Monto ya pagado
                // - payments[]: Array con los pagos (contiene payment_id)
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