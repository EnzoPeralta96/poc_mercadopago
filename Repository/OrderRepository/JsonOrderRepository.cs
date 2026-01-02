using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using poc_mercadopago.Models;

namespace poc_mercadopago.Repository.OrderRepository
{
    public class JsonOrderRepository : IOrderRepository
    {
        private readonly string _filePath;
        private readonly SemaphoreSlim _mutex = new(1, 1);
        public JsonOrderRepository(string filePath = "Data/orders.json")
        {
            _filePath = filePath;
            Directory.CreateDirectory(Path.GetDirectoryName(_filePath));
            if (!File.Exists(_filePath)) File.WriteAllText(_filePath, "[]");
        }
        public async Task<List<Order>> ReadAllAsync(CancellationToken ct)
        {
            var json = await File.ReadAllTextAsync(_filePath, ct);
            var orders = JsonSerializer.Deserialize<List<Order>>(json) ?? [];
            return orders;
        }
        public async Task WriteAllAsync(List<Order> orders, CancellationToken ct)
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };
            var json = JsonSerializer.Serialize(orders, options);
            await File.WriteAllTextAsync(_filePath, json, ct);
        }

        public async Task AddAsync(Order order, CancellationToken ct = default)
        {
            await _mutex.WaitAsync(ct);
            try
            {
                var list = await ReadAllAsync(ct);
                list.Add(order);
                await WriteAllAsync(list, ct);
            }
            finally { _mutex.Release(); }
        }

        public async Task<Order> GetByIdAsync(string id, CancellationToken ct = default)
        {
            await _mutex.WaitAsync(ct);
            try
            {
                var list = await ReadAllAsync(ct);
                return list.FirstOrDefault(o => o.Id == id);
            }
            finally { _mutex.Release(); }
        }

        public async Task<Order> GetByExternalReferenceAsync(string externalReference, CancellationToken ct = default)
        {
            return await GetByIdAsync(externalReference, ct);
        }

        public async Task UpdateAsync(Order order, CancellationToken ct = default)
        {
            await _mutex.WaitAsync(ct);
            try
            {
                var list = await ReadAllAsync(ct);
                var idx = list.FindIndex(o => o.Id == order.Id);
                if (idx < 0) throw new InvalidOperationException($"Order not found: {order.Id}");
                list[idx] = order;
                await WriteAllAsync(list, ct);
            }
            finally { _mutex.Release(); }
        }

    }
}