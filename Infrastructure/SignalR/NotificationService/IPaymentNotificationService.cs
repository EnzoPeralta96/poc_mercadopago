
namespace poc_mercadopago.Infrastructure.SignalR.NotificationService
{
    public interface IPaymentNotificationService
    {
        Task NotifyPaymentCompletdedAsync(string orderId, string status, long? payment_id);
    
    }
}