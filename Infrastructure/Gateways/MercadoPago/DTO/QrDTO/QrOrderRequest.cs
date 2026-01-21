using System.Text.Json.Serialization;

namespace poc_mercadopago.Infrastructure.Gateways.MercadoPago.DTO.QrDTO
{
    public sealed record QrOrderRequest
    {
        [JsonPropertyName("external_reference")]
        public string ExternalReference { get; init; } = string.Empty;

        [JsonPropertyName("title")]
        public string Title { get; init; } = string.Empty;

        [JsonPropertyName("description")]
        public string Description { get; init; } = string.Empty;

        [JsonPropertyName("notification_url")]
        public string NotificationUrl { get; init; } = string.Empty;

        [JsonPropertyName("total_amount")]
        public int TotalAmount { get; init; }

        [JsonPropertyName("items")]
        public List<QrOrderItemRequest> Items { get; set; }

    }


}