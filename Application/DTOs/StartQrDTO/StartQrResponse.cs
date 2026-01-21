using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace poc_mercadopago.Application.DTOs.StartQrDTO
{
    public sealed class StartQrResponse
    {
        public string OrderId { get; init; }
        public string InStoreOrderId{ get; init; }
        public string QrData { get; init; } = string.Empty;
        public string QrImageBase64 { get; init; } = string.Empty;
        public decimal Total { get; init; }
       public string CurrencyId { get; init; } = "ARS";
    }
}