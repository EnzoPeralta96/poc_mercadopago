using poc_mercadopago.Infrastructure.Webhooks.MercadoPago.DTOs;

namespace poc_mercadopago.Infrastructure.Webhooks.MercadoPago.Handlers
{
    /// <summary>
    /// Interface base para handlers de webhooks.
    /// Cada tipo de notificación (payment, merchant_order, etc.)
    /// tiene su propio handler.
    /// </summary>
    public interface IWebhookHandler
    {
        /// <summary>
        /// Tipo de app que este handler procesa ("checkout", "qr").
        /// </summary>
        string AppType { get; }

        /// <summary>
        /// Procesa la notificación.
        /// </summary>
        /// <param name="notification">Datos de la notificación</param>
        /// <returns>Resultado del procesamiento</returns>
        Task<WebhookProcessingResult> HandleAsync(WebhookNotification notification);
    }
}
