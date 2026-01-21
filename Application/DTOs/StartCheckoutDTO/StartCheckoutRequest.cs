namespace poc_mercadopago.Application.DTOs.StartCheckoutDTO
{
    public sealed record StartCheckoutRequest{
        public List<CartItemRequestDTO> Items = [];
    }    
}