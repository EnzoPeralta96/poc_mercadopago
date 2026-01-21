using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace poc_mercadopago.Infrastructure.Gateways.MercadoPago.DTO
{
    //DTO que representa los detalles de un pago obtenido de mercadopago
    public sealed record PaymentDetailsDTO
    {
        public long PaymentId { get; init; } 
        public string Status { get; init; }
        public string OrderId {get; init;}
        public decimal Amount { get; init; }
        public string CurrencyId {get; init;}
    }

}