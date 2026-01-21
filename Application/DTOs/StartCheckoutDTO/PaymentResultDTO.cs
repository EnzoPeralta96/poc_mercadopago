namespace poc_mercadopago.Application.DTOs.StartCheckoutDTO
{
    public class PaymentResultDTO
    {
        public long PaymentId { get; set; }
        public string? Status { get; set; }
        public string? OrderId { get; set; }
        public decimal Amount { get; set; }
        public string? CurrencyId { get; set; }
    }
}