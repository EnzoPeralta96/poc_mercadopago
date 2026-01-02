using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace poc_mercadopago.Presentation.ViewModels.CartViewModels
{
    public class CartRequestViewModel
    {
        [Required]
        public string ProductId { get; set; } = default!;

        [Required]
        public int Quantity { get; set; }
    }
}