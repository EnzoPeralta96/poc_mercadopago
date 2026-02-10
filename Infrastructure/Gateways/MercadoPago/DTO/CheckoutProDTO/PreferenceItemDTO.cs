using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace poc_mercadopago.Infrastructure.Gateways.MercadoPago.DTO
{
    public sealed record PreferenceItemDTO
    {
        public string Id {get; set;} = string.Empty;
        public string Title {get; init;} = string.Empty;
        public string Description {get; init;} = string.Empty;
        public string PictureUrl {get; init;} = string.Empty;
        public int Quantity {get; init;}
        public decimal UnitPrice {get; init;}
        public string CurrencyId {get; init;} = "ARS";
    }
}