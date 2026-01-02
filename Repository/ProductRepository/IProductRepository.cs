using poc_mercadopago.Models;

namespace poc_mercadopago.Repository.ProductRepository;

public interface IProductRepository
{
    Task<IReadOnlyList<Product>> GetAllAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Product>> GetByIdsAsync(List<string> ids);
    Task<Product> GetByIdAsync(string id, CancellationToken ct = default);
}
