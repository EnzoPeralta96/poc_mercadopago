using System.Text.Json.Serialization;

namespace poc_mercadopago.Infrastructure.Gateways.MercadoPago.DTO.QrDTO
{
    /// <summary>
    /// Representa un item dentro de una orden QR.
    /// Cada item se muestra en la app de MP cuando el usuario confirma el pago.
    ///
    /// Los campos usan snake_case via JsonPropertyName para coincidir con la API de MP.
    /// </summary>
    public class QrOrderItemRequest
    {
        /// <summary>
        /// SKU o código interno del producto en nuestro sistema.
        /// Se usa para identificar el producto en reportes y conciliación.
        /// </summary>
        [JsonPropertyName("sku_number")]
        public string SkuNumber { get; set; }

        /// <summary>
        /// Categoría del producto.
        /// Valores comunes: "general", "electronics", "food", etc.
        /// </summary>
        [JsonPropertyName("category")]
        public string Category { get; set; }

        /// <summary>
        /// Nombre del producto visible en la app de MP.
        /// </summary>
        [JsonPropertyName("title")]
        public string Title { get; set; }

        /// <summary>
        /// Descripción del producto.
        /// </summary>
        [JsonPropertyName("description")]
        public string Description { get; set; }

        /// <summary>
        /// Precio unitario del producto.
        /// IMPORTANTE: Debe ser entero para ARS (sin decimales).
        /// </summary>
        [JsonPropertyName("unit_price")]
        public int UnitPrice { get; set; }

        /// <summary>
        /// Cantidad de unidades de este producto.
        /// </summary>
        [JsonPropertyName("quantity")]
        public int Quantity { get; set; }

        /// <summary>
        /// Unidad de medida.
        /// Valor típico: "unit" para productos individuales.
        /// Otros valores: "kg", "lt", etc.
        /// </summary>
        [JsonPropertyName("unit_measure")]
        public string UnitMeasure { get; set; }

        /// <summary>
        /// Total del item (unit_price * quantity).
        /// Debe ser entero para ARS.
        /// </summary>
        [JsonPropertyName("total_amount")]
        public int TotalAmount { get; set; }
    }

}