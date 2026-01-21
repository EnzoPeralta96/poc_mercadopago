using System.Text.Json.Serialization;

namespace poc_mercadopago.Infrastructure.Gateways.MercadoPago.DTO.QrDTO
{
    public sealed record QrOrderResponse
    {
        [JsonPropertyName("qr_data")]
        public string QrData { get; set; }

        [JsonPropertyName("in_store_order_id")]
        public string InStoreOrderId { get; set; }
    }


}