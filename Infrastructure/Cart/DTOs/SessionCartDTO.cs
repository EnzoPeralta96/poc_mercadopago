namespace poc_mercadopago.Infrastructure.Cart.DTOs
{
    public sealed class SessionCartDTO
    {
        public List<CartItemDTO> Items { get; set; } = [];
    }
}