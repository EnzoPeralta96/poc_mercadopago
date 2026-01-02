using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace poc_mercadopago.Presentation.ViewModels.CheckoutViewModels
{
    public class PaymentResultViewModel
    {
        public string Result { get; set; } = default!; // success, failure, pending
        public string? PaymentId { get; set; }
        public string? Status { get; set; }

        // Helpers
        public bool IsSuccess => Result?.ToLower() == "success";
        public bool IsFailure => Result?.ToLower() == "failure";
        public bool IsPending => Result?.ToLower() == "pending";
    }
}