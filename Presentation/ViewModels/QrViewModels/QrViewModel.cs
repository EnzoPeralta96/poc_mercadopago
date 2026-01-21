namespace poc_mercadopago.Presentation.ViewModels.QrViewModels
{
    public class QrViewModel
    {
        public string OrderId {get; set;} = string.Empty;
        public string QrImageBase64 {get; set;} = string.Empty;
        public decimal Total {get; set;}
        public string CurrencyId {get;set;} = "ARS";
        public string InStoreOrderId {get; set;} = string.Empty;
    }
}