
namespace poc_mercadopago.Models
{
    public class Product
    {
        public string Id {get; init;}
        public string Name {get; init;}
        public string Description {get; init;}
        public decimal Price {get; init;}
        public string CurrencyId {get; init;} = "ARS";
    }
}