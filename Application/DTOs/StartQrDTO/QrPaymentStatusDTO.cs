using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace poc_mercadopago.Application.DTOs.StartQrDTO
{
    public sealed record QrPaymentStatusDTO
    {
        public string OrderId { get; init; } = string.Empty;
        public string InStoreOrderId { get; init; }
        public string Status { get; init; } = string.Empty;
        public string OrderStatus { get; init; } = string.Empty;
        public long? PaymentId { get; init; }
        public decimal Total { get; init; }
        public bool IsPaid => Status.Equals("paid", StringComparison.OrdinalIgnoreCase);
        public bool IsPending => Status.Equals("pending", StringComparison.OrdinalIgnoreCase);
        public bool IsCancelled => Status.Equals("cancelled", StringComparison.OrdinalIgnoreCase);
    }
}