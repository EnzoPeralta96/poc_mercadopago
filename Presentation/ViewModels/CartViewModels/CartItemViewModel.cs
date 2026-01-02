namespace poc_mercadopago.Presentation.ViewModels.CartViewModels
{
    public class CartItemViewModel
    {
        public string ProductId { get; set; } = default!;
        public string ProductName { get; set; } = default!;
        public string? ProductDescription { get; set; }
        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; }
        public string CurrencyId { get; set; } = "ARS";
        public decimal Subtotal => UnitPrice * Quantity;
    }
}