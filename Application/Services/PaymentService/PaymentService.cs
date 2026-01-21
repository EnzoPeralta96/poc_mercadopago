using Microsoft.Extensions.Options;
using poc_mercadopago.Application.DTOs;
using poc_mercadopago.Application.DTOs.StartCheckoutDTO;
using poc_mercadopago.Application.DTOs.StartQrDTO;
using poc_mercadopago.Infrastructure.Configuration;
using poc_mercadopago.Infrastructure.Gateways.MercadoPago.Configuration;
using poc_mercadopago.Infrastructure.Gateways.MercadoPago.DTO;
using poc_mercadopago.Infrastructure.Gateways.MercadoPago.DTO.QrDTO;
using poc_mercadopago.Infrastructure.Gateways.MercadoPago.MercadoPagoGateway;
using poc_mercadopago.Infrastructure.Gateways.MercadoPago.MercadoPagoQRGateway;
using poc_mercadopago.Infrastructure.QRCode;
using poc_mercadopago.Models;
using poc_mercadopago.Repository.OrderRepository;
using poc_mercadopago.Repository.ProductRepository;

namespace poc_mercadopago.Application.Services.PaymentService
{
    public class PaymentService : IPaymentService
    {
        private readonly ILogger<PaymentService> _logger;
        private readonly IProductRepository _productRepository;
        private readonly IOrderRepository _orderRepository;
        private readonly IMercadoPagoGateway _gateway;
        private readonly IMercadoPagoQRGateway _qRGateway;
        private readonly MercadoPagoOptions _mercadoPagoOptions;
        private readonly MercadoPagoQrOptions _mercadoPagoQrOptions;
        private readonly IQrCodeGenerator _qrCodeGenerator;

        public PaymentService(
            ILogger<PaymentService> logger,
            IProductRepository productRepository, IOrderRepository orderRepository,
            IMercadoPagoGateway gateway, IMercadoPagoQRGateway qRGateway,
            IOptions<MercadoPagoOptions> mpOptions, IOptions<MercadoPagoQrOptions> mpQrOptions,
            IQrCodeGenerator qrCodeGenerator)
        {
            _logger = logger;
            _productRepository = productRepository;
            _orderRepository = orderRepository;
            _gateway = gateway;
            _qRGateway = qRGateway;
            _mercadoPagoOptions = mpOptions.Value;
            _mercadoPagoQrOptions = mpQrOptions.Value;
            _qrCodeGenerator = qrCodeGenerator;
        }


        private Order CreateOrder(List<CartItemRequestDTO> items, IReadOnlyList<Product> productsSelected)
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

        /// <summary>
        /// Mapea el estado de merchant order de MP al estado de orden local.
        /// Estados de merchant order:
        /// - "opened": Esperando pago
        /// - "paid": Pagado completamente
        /// - "partially_paid": Pagado parcialmente
        /// - "cancelled": Cancelado
        /// - "expired": Expirado
        /// </summary>
        private OrderStatus MapMerchantOrderStatusToOrderStatus(string? merchantOrderStatus)
        {
            return merchantOrderStatus?.ToLower() switch
            {
                "paid" => OrderStatus.approved,
                "opened" => OrderStatus.Pending,
                "partially_paid" => OrderStatus.Pending,
                "cancelled" => OrderStatus.Rejected,
                "expired" => OrderStatus.Rejected,
                _ => OrderStatus.Pending
            };
        }


        private CreatePreferenceRequest CreatePreferenceCheckoutPro(Order order)
        {
            return new CreatePreferenceRequest
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
        }

        private QrOrderRequest CreateQrOrder(Order order)
        {
            //Preparar y enviar la solicitud de creación de orden QR a MercadoPago
            return new QrOrderRequest
            {
                ExternalReference = order.Id,
                Title = order.Title,
                Description = $"Compra de {order.Items.Count} productos",
                NotificationUrl = $"{_mercadoPagoQrOptions.BaseUrl}/webhooks/mercadopago/qr",
                TotalAmount = (int)order.Total,
                Items = order.Items.Select(item => new QrOrderItemRequest
                {
                    SkuNumber = item.ProductId,
                    Category = "general",
                    Title = item.Title,
                    Description = item.Title,
                    UnitPrice = (int)item.UnitPrice,
                    Quantity = item.Quantity,
                    UnitMeasure = "unit",
                    TotalAmount = (int)item.SubTotal
                }).ToList()
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

                CreatePreferenceRequest preferenceRequest = CreatePreferenceCheckoutPro(order);

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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al procesar checkout para {ItemCount} items", request.Items.Count);
                return null;
            }
        }



        public async Task<StartQrResponse> StartQrAsync(StartQrRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                //Validar que los productos existan
                var productsIds = request.Items.Select(i => i.ProductId).ToList();

                var productsSelected = await _productRepository.GetByIdsAsync(productsIds);

                bool allProductsSelectedExists = productsIds.Count == productsSelected.Count;

                if (!allProductsSelectedExists)
                {
                    _logger.LogWarning("No todos los productos existen. Esperados: {Expected}, Encontrados: {Found}",
                           productsIds.Count, productsSelected.Count);

                    return null;
                }

                //Crear y persistir la orden de forma local
                var order = CreateOrder(request.Items, productsSelected);

                await _orderRepository.AddAsync(order);

                _logger.LogInformation(
                    "Orden creada: {OrderId} - Total: {Total}",
                    order.Id,
                    order.Total
                );

                QrOrderRequest qrRequest = CreateQrOrder(order);

                var qrOrderResponse = await _qRGateway.CreateQrOrderAsync(qrRequest);

                if (qrOrderResponse is null)
                {
                    _logger.LogError("Error al crear orden QR en MercadoPago para la orden {OrderId}", order.Id);
                    return null;
                }

                string qrImageBase64 = _qrCodeGenerator.GenerateQrImageBase64(qrOrderResponse.QrData);


                order.MercadoPagoInStoreOrderId = qrOrderResponse.InStoreOrderId;

                await _orderRepository.UpdateAsync(order);

                return new StartQrResponse
                {
                    OrderId = order.Id,
                    InStoreOrderId = qrOrderResponse.InStoreOrderId,
                    QrData = qrOrderResponse.QrData,
                    QrImageBase64 = qrImageBase64,
                    Total = order.Total,
                    CurrencyId = order.CurrencyId
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al procesar checkout QR para {ItemCount} items", request.Items.Count);
                return null;
            }
        }


        public async Task<QrPaymentStatusDTO> GetQrPaymentStatusAsync(string orderId, CancellationToken cancellationToken = default)
        {
            try
            {
                var order = await _orderRepository.GetByIdAsync(orderId);

                if (order is null)
                {
                    _logger.LogWarning("No se encontró la orden con Id :0{OrderId}", orderId);
                    return null;
                }

                if (string.IsNullOrEmpty(order.MercadoPagoInStoreOrderId))
                {
                    _logger.LogWarning("Orden {OrderId} no tiene InStoreOrderId", orderId);
                    return null;
                }

                var merchantOrderStatus = await _qRGateway.GetMerchantOrderStatusAsync(order.MercadoPagoInStoreOrderId);

                var newStatus = MapMerchantOrderStatusToOrderStatus(merchantOrderStatus.Status);

                if (order.Status != newStatus)
                {
                    order.Status = newStatus;

                    if (merchantOrderStatus.Payments.Count > 0)
                    {
                        order.MercadoPagoPaymentId = merchantOrderStatus.Payments.First().Id;
                    }

                    await _orderRepository.UpdateAsync(order);
                }

                return new QrPaymentStatusDTO
                {
                    OrderId = order.Id,
                    InStoreOrderId = order.MercadoPagoInStoreOrderId,
                    Status = merchantOrderStatus.Status,
                    OrderStatus = order.Status.ToString(),
                    PaymentId = order.MercadoPagoPaymentId,
                    Total = order.Total
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener estado de pago QR para orden {OrderId}", orderId);
                return null;
            }
        }

        public async Task<QrPaymentStatusDTO> ProcessMerchantOrderWebhookAsync(long merchantOrderId, CancellationToken cancellationToken = default)
        {
            try
            {
                // 1. Consultar a MercadoPago por el merchant_order_id
                var merchantOrderStatus = await _qRGateway.GetMerchantOrderStatusAsync(merchantOrderId.ToString(), cancellationToken);

                if (merchantOrderStatus is null)
                {
                    _logger.LogWarning("No se pudo obtener merchant order {MerchantOrderId} desde MercadoPago", merchantOrderId);
                    return null;
                }

                // 2. Extraer el external_reference (nuestro OrderId local)
                var localOrderId = merchantOrderStatus.ExternalReference;

                if (string.IsNullOrEmpty(localOrderId))
                {
                    _logger.LogWarning("Merchant order {MerchantOrderId} no tiene external_reference", merchantOrderId);
                    return null;
                }

                // 3. Buscar la orden local
                var order = await _orderRepository.GetByIdAsync(localOrderId);

                if (order is null)
                {
                    _logger.LogWarning("No se encontró la orden local con Id: {OrderId} (MerchantOrderId: {MerchantOrderId})",
                        localOrderId, merchantOrderId);
                    return null;
                }

                // 4. Actualizar el estado de la orden
                var newStatus = MapMerchantOrderStatusToOrderStatus(merchantOrderStatus.Status);

                if (order.Status != newStatus)
                {
                    order.Status = newStatus;

                    if (merchantOrderStatus.Payments.Count > 0)
                    {
                        order.MercadoPagoPaymentId = merchantOrderStatus.Payments.First().Id;
                    }

                    await _orderRepository.UpdateAsync(order);

                    _logger.LogInformation(
                        "Orden {OrderId} actualizada a estado {Status} desde webhook MerchantOrderId: {MerchantOrderId}",
                        order.Id, newStatus, merchantOrderId);
                }

                return new QrPaymentStatusDTO
                {
                    OrderId = order.Id,
                    InStoreOrderId = order.MercadoPagoInStoreOrderId,
                    Status = merchantOrderStatus.Status,
                    OrderStatus = order.Status.ToString(),
                    PaymentId = order.MercadoPagoPaymentId,
                    Total = order.Total
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al procesar webhook para MerchantOrderId {MerchantOrderId}", merchantOrderId);
                return null;
            }
        }
    }
}