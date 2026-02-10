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
                Description = productsSelected[index].Description,
                ImageUrl = productsSelected[index].ImageUrl,
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
                    Id = i.ProductId,
                    Title = i.Title,
                    Description = i.Description,
                    PictureUrl = i.ImageUrl,
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice,
                    CurrencyId = i.CurrencyId
                }).ToList()
            };
        }

        /// <summary>
        /// Crea el objeto QrOrderRequest a partir de una Order local.
        ///
        /// IMPORTANTE - Campos críticos:
        /// - external_reference: Nuestro OrderId. Este es el ÚNICO vínculo entre MP y nuestro sistema.
        ///   Cuando llega el webhook, usamos este campo para encontrar la orden local.
        /// - notification_url: URL donde MP enviará los webhooks. Debe ser accesible desde internet.
        ///   En desarrollo se usa ngrok para exponer el servidor local.
        /// - total_amount: Monto total en enteros (sin decimales para ARS).
        ///
        /// NOTA sobre "sponsor": En modo Sandbox/Test Users NO incluir el campo sponsor.
        /// Si se incluye, genera el error "El user del sponsor y del collector deben ser de tipos iguales".
        /// </summary>
        private QrOrderRequest CreateQrOrder(Order order)
        {
            // Construir el request para crear la orden QR en Mercado Pago
            return new QrOrderRequest
            {
                // CRÍTICO: Este es nuestro OrderId local que nos permite reconciliar el pago
                ExternalReference = order.Id,

                // Título y descripción visibles en la app de MP cuando el usuario escanea
                Title = order.Title,
                Description = $"Compra de {order.Items.Count} productos",

                // URL del webhook - MP enviará notificaciones de tipo "merchant_order" aquí
                // Debe ser HTTPS y accesible desde internet (usar ngrok en desarrollo)
                NotificationUrl = $"{_mercadoPagoQrOptions.BaseUrl}/webhooks/mercadopago/qr",

                // El monto debe ser entero para pesos argentinos (sin decimales)
                TotalAmount = (int)order.Total,

                // Items de la orden con sus detalles
                Items = order.Items.Select(item => new QrOrderItemRequest
                {
                    SkuNumber = item.ProductId,      // SKU o ID del producto
                    Category = "general",            // Categoría del producto
                    Title = item.Title,              // Nombre visible del producto
                    Description = item.Title,        // Descripción del producto
                    UnitPrice = (int)item.UnitPrice, // Precio unitario (entero)
                    Quantity = item.Quantity,        // Cantidad
                    UnitMeasure = "unit",            // Unidad de medida
                    TotalAmount = (int)item.SubTotal // Subtotal del item (precio * cantidad)
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



        /// <summary>
        /// Inicia el flujo de pago con QR dinámico.
        ///
        /// Flujo completo:
        /// 1. Validar que todos los productos del carrito existen
        /// 2. Crear y persistir la orden local con estado "Pending"
        /// 3. Llamar a la API de MP para crear la orden QR (POST /instore/orders/qr/...)
        /// 4. Generar imagen QR a partir del qr_data de MP
        /// 5. Actualizar la orden con el in_store_order_id de MP
        /// 6. Retornar los datos para mostrar el QR al usuario
        ///
        /// El cliente luego:
        /// - Muestra el QR al usuario
        /// - Inicia conexión SignalR para recibir notificación de pago
        /// - El usuario escanea con la app de MP y paga
        /// - El webhook notifica al servidor y SignalR al cliente
        /// </summary>
        public async Task<StartQrResponse> StartQrAsync(StartQrRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                // PASO 1: Validar que todos los productos existen en la base de datos
                var productsIds = request.Items.Select(i => i.ProductId).ToList();
                var productsSelected = await _productRepository.GetByIdsAsync(productsIds);

                bool allProductsSelectedExists = productsIds.Count == productsSelected.Count;

                if (!allProductsSelectedExists)
                {
                    _logger.LogWarning("No todos los productos existen. Esperados: {Expected}, Encontrados: {Found}",
                           productsIds.Count, productsSelected.Count);
                    return null;
                }

                // PASO 2: Crear y persistir la orden local con estado "Pending"
                // El OrderId generado aquí será usado como external_reference en MP
                var order = CreateOrder(request.Items, productsSelected);
                await _orderRepository.AddAsync(order);

                _logger.LogInformation(
                    "Orden creada: {OrderId} - Total: {Total}",
                    order.Id,
                    order.Total
                );

                // PASO 3: Crear la orden QR en Mercado Pago
                // Esto llama a POST /instore/orders/qr/seller/collectors/{user_id}/pos/{pos_id}/qrs
                QrOrderRequest qrRequest = CreateQrOrder(order);
                var qrOrderResponse = await _qRGateway.CreateQrOrderAsync(qrRequest);

                if (qrOrderResponse is null)
                {
                    _logger.LogError("Error al crear orden QR en MercadoPago para la orden {OrderId}", order.Id);
                    return null;
                }

                // PASO 4: Generar la imagen QR a partir del qr_data (string EMV)
                // El qr_data es un string codificado que contiene toda la info del pago
                // Lo convertimos a imagen PNG en base64 para mostrar en el HTML
                string qrImageBase64 = _qrCodeGenerator.GenerateQrImageBase64(qrOrderResponse.QrData);

                // PASO 5: Actualizar la orden con el ID de MP para futura referencia
                order.MercadoPagoInStoreOrderId = qrOrderResponse.InStoreOrderId;
                await _orderRepository.UpdateAsync(order);

                // PASO 6: Retornar los datos para mostrar el QR al usuario
                return new StartQrResponse
                {
                    OrderId = order.Id,                          // Nuestro ID local
                    InStoreOrderId = qrOrderResponse.InStoreOrderId, // ID de MP
                    QrData = qrOrderResponse.QrData,             // String EMV (por si se necesita)
                    QrImageBase64 = qrImageBase64,               // Imagen lista para <img src="...">
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
        /// <summary>
        /// Procesa una notificación de merchant order desde el webhook de MercadoPago.
        ///
        /// Este es el método más importante del flujo de verificación de pagos QR.
        ///
        /// CONTEXTO CRÍTICO:
        /// - El webhook de MP envía el merchant_order_id (ID de MP, ej: 37461186157)
        /// - Este ID NO es nuestro OrderId local
        /// - Debemos consultar a MP para obtener el external_reference que contiene nuestro OrderId
        ///
        /// Flujo:
        /// 1. Recibir merchant_order_id del webhook
        /// 2. Consultar GET /merchant_orders/{id} a MP
        /// 3. Extraer el external_reference (nuestro OrderId)
        /// 4. Buscar la orden local en nuestra base de datos
        /// 5. Actualizar el estado según el status de la merchant order
        /// 6. Retornar datos para notificar al cliente vía SignalR
        ///
        /// Estados de merchant_order:
        /// - "opened": Esperando pago
        /// - "closed": Pagado completamente (PaidAmount >= TotalAmount)
        /// - "expired": Expirada sin pago
        /// </summary>
        public async Task<QrPaymentStatusDTO> ProcessMerchantOrderWebhookAsync(long merchantOrderId, CancellationToken cancellationToken = default)
        {
            try
            {
                // PASO 1: Consultar a MercadoPago para obtener los datos completos
                // El webhook solo envía el ID, necesitamos el external_reference y status
                var merchantOrderStatus = await _qRGateway.GetMerchantOrderStatusAsync(merchantOrderId.ToString(), cancellationToken);

                if (merchantOrderStatus is null)
                {
                    _logger.LogWarning("No se pudo obtener merchant order {MerchantOrderId} desde MercadoPago", merchantOrderId);
                    return null;
                }

                // PASO 2: Extraer el external_reference (nuestro OrderId local)
                // Este es el vínculo crítico entre el sistema de MP y el nuestro
                var localOrderId = merchantOrderStatus.ExternalReference;

                if (string.IsNullOrEmpty(localOrderId))
                {
                    _logger.LogWarning("Merchant order {MerchantOrderId} no tiene external_reference", merchantOrderId);
                    return null;
                }

                // PASO 3: Buscar la orden en nuestra base de datos usando el external_reference
                var order = await _orderRepository.GetByIdAsync(localOrderId);

                if (order is null)
                {
                    _logger.LogWarning("No se encontró la orden local con Id: {OrderId} (MerchantOrderId: {MerchantOrderId})",
                        localOrderId, merchantOrderId);
                    return null;
                }

                // PASO 4: Mapear el estado de MP a nuestro estado local
                // IMPORTANTE: "closed" en merchant_order significa PAGADO
                var newStatus = MapMerchantOrderStatusToOrderStatus(merchantOrderStatus.Status);

                // PASO 5: Actualizar el estado si cambió
                if (order.Status != newStatus)
                {
                    order.Status = newStatus;

                    // Guardar el payment_id si hay pagos asociados
                    // El payment_id es útil para consultas posteriores o reembolsos
                    if (merchantOrderStatus.Payments.Count > 0)
                    {
                        order.MercadoPagoPaymentId = merchantOrderStatus.Payments.First().Id;
                    }

                    await _orderRepository.UpdateAsync(order);

                    _logger.LogInformation(
                        "Orden {OrderId} actualizada a estado {Status} desde webhook MerchantOrderId: {MerchantOrderId}",
                        order.Id, newStatus, merchantOrderId);
                }

                // PASO 6: Retornar datos para notificar al cliente vía SignalR
                // El Status aquí es el de MP ("closed" = pagado), no el mapeado
                return new QrPaymentStatusDTO
                {
                    OrderId = order.Id,                              // Nuestro ID local
                    InStoreOrderId = order.MercadoPagoInStoreOrderId,// ID de orden de MP
                    Status = merchantOrderStatus.Status,             // Estado de MP (usado para SignalR)
                    OrderStatus = order.Status.ToString(),           // Estado local mapeado
                    PaymentId = order.MercadoPagoPaymentId,          // ID del pago en MP
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