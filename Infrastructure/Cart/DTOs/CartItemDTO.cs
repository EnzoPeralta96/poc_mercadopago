using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Threading.Tasks;

namespace poc_mercadopago.Infrastructure.Cart.DTOs
{
    public sealed class CartItemDTO
    {
        public string ProductId {get; set;}
        public int Quantity {get; set;}
    }
}