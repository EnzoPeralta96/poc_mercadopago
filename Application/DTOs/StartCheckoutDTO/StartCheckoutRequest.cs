using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Threading.Tasks;

namespace poc_mercadopago.Application.DTOs.StartCheckoutDTO
{
    public sealed record StartCheckoutRequest{
        public List<CheckoutCartItemDTO> Items = [];
    }    
    public sealed record CheckoutCartItemDTO
    {
        public string ProductId {get; init;}
        public int Quantity {get; init;}
    }
}