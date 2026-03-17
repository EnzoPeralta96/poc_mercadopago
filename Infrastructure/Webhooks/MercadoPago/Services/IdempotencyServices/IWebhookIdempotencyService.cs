namespace poc_mercadopago.Infrastructure.Webhooks.MercadoPago.Services
{
    public interface IWebhookIdempotencyService
    {
        /// <summary>
        /// Verifica si ya procesamos esta notificación.
        /// Si no la vimos, la marca como vista y retorna true (procesar).
        /// Si ya la vimos, retorna false (ignorar).
        /// </summary>
        /// <param name="notificationId">ID único (ej: "payment_123456")</param>
        /// <returns>True si debemos procesar, False si es duplicado</returns>
        Task<bool> TryProcessAsync(string notificationId);

        /// <summary>
        /// Verifica si una notificación ya fue vista (sin marcarla).
        /// <summary>
        Task<bool> HasBeenProcessedAsync(string notificationId);
    }
}