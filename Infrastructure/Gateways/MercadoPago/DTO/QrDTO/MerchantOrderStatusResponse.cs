using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace poc_mercadopago.Infrastructure.Gateways.MercadoPago.DTO.QrDTO
{
    /// <summary>
    /// Response al consultar el estado de una merchant order (orden QR).
    /// Endpoint: GET /merchant_orders/{id}
    /// </summary>
    public sealed record MerchantOrderStatusResponse
    {
        /// <summary>
        /// ID de la merchant order.
        /// </summary>
        [JsonPropertyName("id")]
        public long Id { get; init; }

        /// <summary>
        /// Estado de la orden.
        /// Valores posibles:
        /// - "opened": Esperando pago
        /// - "paid": Pagada completamente
        /// - "partially_paid": Pagada parcialmente
        /// - "cancelled": Cancelada
        /// - "expired": Expirada
        /// </summary>
        [JsonPropertyName("status")]
        public string Status { get; init; } = string.Empty;

        /// <summary>
        /// Referencia externa (nuestro OrderId).
        /// </summary>
        [JsonPropertyName("external_reference")]
        public string ExternalReference { get; init; } = string.Empty;

        /// <summary>
        /// Total de la orden.
        /// </summary>
        [JsonPropertyName("total_amount")]
        public decimal TotalAmount { get; init; }

        /// <summary>
        /// Monto ya pagado.
        /// </summary>
        [JsonPropertyName("paid_amount")]
        public decimal PaidAmount { get; init; }

        /// <summary>
        /// Lista de pagos asociados a esta orden.
        /// Si la orden fue pagada, aquí estará el payment_id.
        /// </summary>
        [JsonPropertyName("payments")]
        public List<MerchantOrderPayment> Payments { get; init; } = [];

        /// <summary>
        /// Fecha de creación.
        /// </summary>
        [JsonPropertyName("date_created")]
        public DateTimeOffset DateCreated { get; init; }
    }

    public sealed record MerchantOrderPayment
    {
        /// <summary>
        /// ID del pago.
        /// </summary>
        [JsonPropertyName("id")]
        public long Id { get; init; }

        /// <summary>
        /// Estado del pago.
        /// Valores: "approved", "pending", "rejected", etc.
        /// </summary>
        [JsonPropertyName("status")]
        public string Status { get; init; } = string.Empty;

        /// <summary>
        /// Monto del pago.
        /// </summary>
        [JsonPropertyName("transaction_amount")]
        public decimal TransactionAmount { get; init; }
    }
}