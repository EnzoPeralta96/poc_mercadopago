namespace poc_mercadopago.Infrastructure.Gateways.MercadoPago.DTO;

public sealed record CreatePreferenceRequest
{
    public string OrderId {get; init;} = string.Empty;
    public List<PreferenceItemDTO> Items {get; init;} = [];
}





