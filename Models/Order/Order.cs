using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace poc_mercadopago.Models
{

    public class Order
    {
        public string Id { get; init; }
        public string Title { get; init; }
        public decimal Total { get; init; }
        public string CurrencyId { get; init; } = "ARS";
        public OrderStatus Status {get; set;} = OrderStatus.Created;
        public DateTimeOffset CreatedAt {get; init;}
        public List<OrderItem> Items {get; init;} = [];
        public string? MercadoPagoPreferenceId {get; set;}
        public long? MercadoPagoPaymentId {get; set;}
    }

}