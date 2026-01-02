using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace poc_mercadopago.Presentation.ViewModels.CartViewModels
{
    public class CartViewModel
    {
        public List<CartItemViewModel> Items { get; set; } = [];
        public decimal Total => Items.Sum(i => i.Subtotal);
        public int TotalItems => Items.Sum(i => i.Quantity);
    }
}