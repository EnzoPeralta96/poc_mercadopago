
using poc_mercadopago.Models;
namespace poc_mercadopago.Repository.OrderRepository
{
    public interface IOrderRepository
    {
        Task AddAsync(Order order, CancellationToken ct = default);
        Task<Order> GetByIdAsync(string id, CancellationToken ct = default);
        Task<Order> GetByExternalReferenceAsync(string externalReference, CancellationToken ct = default);
        Task UpdateAsync(Order order, CancellationToken ct = default);
        Task<List<Order>> ReadAllAsync(CancellationToken ct);
        Task WriteAllAsync(List<Order> orders, CancellationToken ct);
    }
}