using Microsoft.Extensions.Options;
using poc_mercadopago.Application.DTOs.StartCheckoutDTO;
using poc_mercadopago.Infrastructure.Configuration;
using poc_mercadopago.Infrastructure.Gateways.MercadoPago;
using poc_mercadopago.Infrastructure.Gateways.MercadoPago.DTO;
using poc_mercadopago.Models;
using poc_mercadopago.Repository.OrderRepository;
using poc_mercadopago.Repository.ProductRepository;

namespace poc_mercadopago.Application.Services.PaymentService
{
    public class PaymentService : IPaymentService
    {
        private readonly IProductRepository _productRepository;
        private readonly IOrderRepository _orderRepository;
        private readonly IMercadoPagoGateway _gateway;
        private readonly ILogger<PaymentService> _logger;
        private readonly MercadoPagoOptions _mercadoPagoOptions;

        public PaymentService(IProductRepository productRepository, IOrderRepository orderRepository, IMercadoPagoGateway gateway, ILogger<PaymentService> logger, IOptions<MercadoPagoOptions> mpOptions)
        {
            _productRepository = productRepository;
            _orderRepository = orderRepository;
            _gateway = gateway;
            _logger = logger;
            _mercadoPagoOptions = mpOptions.Value;
        }

        private OrderStatus MapPaymentStatusToOrderStatus(string? mercadoPagoStatus)
        {
            return mercadoPagoStatus?.ToLower() switch
            {
                "approved" => OrderStatus.approved,
                "pending" => OrderStatus.Pending,
                "in_process" => OrderStatus.Pending,
                "rejected" => OrderStatus.Rejected,
                "cancelled" => OrderStatus.Rejected,
                "refunded" => OrderStatus.Rejected,
                "charged_back" => OrderStatus.Rejected,
                _ => OrderStatus.Pending // Default para casos no manejados
            };
        }

        public async Task<PaymentResultDTO> GetPaymentResultAsync(long paymentId, CancellationToken cancellationToken = default)
        {
            try
            {
                var paymentDetails = await _gateway.GetPaymentAsync(paymentId);

                if (!string.IsNullOrEmpty(paymentDetails.OrderId))
                {
                    var order = await _orderRepository.GetByIdAsync(paymentDetails.OrderId);
                    if (order is not null)
                    {
                        order.Status = MapPaymentStatusToOrderStatus(paymentDetails.Status);
                        order.MercadoPagoPaymentId = paymentDetails.PaymentId;
                    }
                    await _orderRepository.UpdateAsync(order);
                }

                return new PaymentResultDTO
                {
                    PaymentId = paymentDetails.PaymentId,
                    Status = paymentDetails.Status,
                    OrderId = paymentDetails.OrderId,
                    Amount = paymentDetails.Amount,
                    CurrencyId = paymentDetails.CurrencyId
                };
            }
            catch (Exception ex)
            {
                _logger.LogError("Error al obtener resultado del pago {PaymentId} - Error : {ex}", paymentId, ex);
                return null;
            }
        }

        public async Task<StartCheckoutRespone> StartCheckoutAsync(StartCheckoutRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                var productsIds = request.Items.Select(i => i.ProductId).ToList();
                var productsSelected = await _productRepository.GetByIdsAsync(productsIds);
                bool allProductsSelectedExists = productsIds.Count == productsSelected.Count;

                if (!allProductsSelectedExists) return null;

                var order = CreateOrder(request.Items, productsSelected);

                await _orderRepository.AddAsync(order);

                _logger.LogInformation("Orden creada: {OrderId} - Total: {Total}", order.Id, order.Total);

                var preferenceRequest = new CreatePreferenceRequest
                {
                    OrderId = order.Id,
                    Items = order.Items.Select(i => new PreferenceItemDTO
                    {
                        Title = i.Title,
                        Quantity = i.Quantity,
                        UnitPrice = i.UnitPrice,
                        CurrencyId = i.CurrencyId
                    }).ToList()
                };

                var preferenceId = await _gateway.CreatePreferenceAsync(preferenceRequest);

                order.MercadoPagoPreferenceId = preferenceId;

                await _orderRepository.UpdateAsync(order);

                return new StartCheckoutRespone
                {
                    OrderId = order.Id,
                    PreferenceId = preferenceId,
                    PublicKey = _mercadoPagoOptions.PublicKey,
                    Total = order.Total
                };
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error al procesar checkout para {ItemCount} items", request.Items.Count);
                return null;
            }
        }

        private Order CreateOrder(List<CheckoutCartItemDTO> items, IReadOnlyList<Product> productsSelected)
        {
            var orderId = Guid.NewGuid().ToString("N");

            var orderItems = items.Select((item, index) => new OrderItem
            {
                ProductId = item.ProductId,
                Title = productsSelected[index].Name,
                Quantity = item.Quantity,
                UnitPrice = productsSelected[index].Price,
                CurrencyId = productsSelected[index].CurrencyId
            }).ToList();

            var total = orderItems.Sum(i => i.SubTotal);

            return new Order
            {
                Id = orderId,
                Title = $"Orden {orderId}",
                Total = total,
                CurrencyId = orderItems.First().CurrencyId,
                Status = OrderStatus.Pending,
                Items = orderItems,
                CreatedAt = DateTimeOffset.Now
            };
        }

    }
}