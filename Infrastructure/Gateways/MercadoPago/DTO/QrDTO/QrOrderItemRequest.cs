using System.Text.Json.Serialization;

namespace poc_mercadopago.Infrastructure.Gateways.MercadoPago.DTO.QrDTO
{
    public class QrOrderItemRequest
    {
        [JsonPropertyName("sku_number")]
        public string SkuNumber { get; set; }

        [JsonPropertyName("category")]
        public string Category { get; set; }

        [JsonPropertyName("title")]
        public string Title { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("unit_price")]
        public int UnitPrice { get; set; }

        [JsonPropertyName("quantity")]
        public int Quantity { get; set; }

        [JsonPropertyName("unit_measure")]
        public string UnitMeasure { get; set; }

        [JsonPropertyName("total_amount")]
        public int TotalAmount { get; set; }
    }

}