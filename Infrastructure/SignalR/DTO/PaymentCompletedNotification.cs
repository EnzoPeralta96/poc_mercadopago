namespace poc_mercadopago.Infrastructure.SignalR.DTO
{
    public sealed record PaymentCompletedNotification
    {
        public required string OrderId { get; init; } = string.Empty;
        public required string Status { get; init; } = string.Empty;
        public required long? PaymentId { get; init; }
        public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
        public string Message { get; init; } = string.Empty;
    }
}