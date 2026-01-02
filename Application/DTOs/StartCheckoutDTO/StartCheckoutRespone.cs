namespace poc_mercadopago.Application.DTOs.StartCheckoutDTO
{
    public sealed record StartCheckoutRespone{
        public string OrderId {get; init;}
        public string PreferenceId {get; init;}
        public string PublicKey {get; init;}
        public decimal Total {get; init;}
    }
}