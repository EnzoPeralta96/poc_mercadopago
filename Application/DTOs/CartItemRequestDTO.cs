namespace poc_mercadopago.Application.DTOs
{
    public sealed record CartItemRequestDTO
    {
        public string ProductId {get; init;}
        public int Quantity {get; init;}
    }
}