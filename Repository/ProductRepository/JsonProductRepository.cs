using System.Text.Json;
using poc_mercadopago.Models;

namespace poc_mercadopago.Repository.ProductRepository;

public class JsonProductRepository : IProductRepository
{
    private readonly string _filePath;

    public JsonProductRepository(string filePath = "Data/products.json")
    {
        _filePath = filePath;
    }

    public async Task<IReadOnlyList<Product>> GetAllAsync(CancellationToken ct = default)
    {
        var json = await File.ReadAllTextAsync(_filePath, ct);
        var products = JsonSerializer.Deserialize<List<Product>>(
            json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
        );
        return products ?? new List<Product>();
    }

    public async Task<Product> GetByIdAsync(string id, CancellationToken ct = default)
    {
        var products = await GetAllAsync(ct);
        return products.FirstOrDefault(p => p.Id == id);
    }

    public async Task<IReadOnlyList<Product>> GetByIdsAsync(List<string> ids)
    {
        var products = await GetAllAsync();
        return products
                .Where(p => ids.Contains(p.Id))
                .ToList();
    }
}
