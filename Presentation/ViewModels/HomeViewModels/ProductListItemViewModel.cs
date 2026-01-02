namespace poc_mercadopago.Presentation.ViewModels
{
    public sealed record ProductListItemViewModel
    (
        string Id,
        string Name,
        string? Description,
        decimal Price,
        string CurrencyId
    );


}