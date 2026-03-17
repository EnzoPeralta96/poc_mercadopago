namespace poc_mercadopago.Models
{
    public class OrderItem
    {
        public string ProductId { get; init; }
        public string Title { get; init; }
        public string Description { get; init; }
        public string ImageUrl {get; init;}
        public int Quantity { get; init; }
        public decimal UnitPrice { get; init; }
        public string CurrencyId { get; init; } = "ARS";
        public decimal SubTotal => Quantity * UnitPrice;
    }

}