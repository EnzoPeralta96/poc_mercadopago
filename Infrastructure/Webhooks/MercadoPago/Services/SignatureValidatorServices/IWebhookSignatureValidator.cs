using poc_mercadopago.Infrastructure.Webhooks.MercadoPago.DTOs;

namespace poc_mercadopago.Infrastructure.Webhooks.MercadoPago.Services
{
    public interface IWebhookSignatureValidator
    {
        /// <summary>
        /// Valida la firma de un webhook.
        /// </summary>
        /// <param name="xSignature">Header x-signature (ej: "ts=123,v1=abc...")</param>
        /// <param name="xRequestId">Header x-request-id</param>
        /// <param name="dataId">El data.id del query string</param>
        /// <returns>True si la firma es válida, False si no</returns>
        /// <param name="appType">Tipo de app que envió el webhook ("checkout" o "qr"), viene del query param ?appType=</param>
        bool Validate(string? appType, string? xSignature, string? xRequestId, string? dataId);
    }
}