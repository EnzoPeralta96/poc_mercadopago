using Microsoft.AspNetCore.Mvc;
using poc_mercadopago.Application.DTOs;
using poc_mercadopago.Application.DTOs.StartCheckoutDTO;
using poc_mercadopago.Application.DTOs.StartQrDTO;
using poc_mercadopago.Application.Services.PaymentService;
using poc_mercadopago.Infrastructure.Cart.CartStore;
using poc_mercadopago.Presentation.ViewModels.CheckoutViewModels;
using poc_mercadopago.Presentation.ViewModels.QrViewModels;

namespace poc_mercadopago.Controllers
{
    /// <summary>
    /// Controlador que maneja los flujos de pago con Mercado Pago.
    ///
    /// Endpoints disponibles:
    /// - POST /MercadoPago/Checkout: Inicia pago con Checkout Pro (wallet/redirect)
    /// - POST /MercadoPago/PaymentQr: Inicia pago con QR dinámico
    /// - GET /checkout/return/{result}: Página de resultado del pago
    ///
    /// Este controlador es llamado desde JavaScript (cart.js) mediante AJAX
    /// y retorna vistas parciales que se cargan en el offcanvas del carrito.
    /// </summary>
    public class MercadoPagoController : Controller
    {
        private readonly IPaymentService _paymentService;
        private readonly ICartStore _cartStore;
        private readonly ILogger<MercadoPagoController> _logger;

        public MercadoPagoController(ILogger<MercadoPagoController> logger, IPaymentService paymentService, ICartStore cartStore)
        {
            _logger = logger;
            _paymentService = paymentService;
            _cartStore = cartStore;
        }

        /// <summary>
        /// Obtiene los items del carrito actual y los convierte en DTOs para el servicio de pago.
        /// Este método es compartido entre Checkout Pro y QR.
        /// </summary>
        private async Task<List<CartItemRequestDTO>> GetOrderItemsAsync()
        {
            var cart = await _cartStore.GetCartAsync();
            if (!cart.Items.Any()) return [];

            return cart.Items.Select(item => new CartItemRequestDTO
            {
                ProductId = item.ProductId,
                Quantity = item.Quantity
            }).ToList();
        }


        [HttpPost]
        public async Task<IActionResult> Checkout()
        {
            var checkoutItems = await GetOrderItemsAsync();
            // Validar que no esté vacío
            if (!checkoutItems.Any()) return PartialView("~/Presentation/Views/Cart/_CartEmpty.cshtml");

            var request = new StartCheckoutRequest
            {
                Items = checkoutItems //id y cant
            };

            var result = await _paymentService.StartCheckoutAsync(request);

            if (result is null)
                return PartialView("~/Presentation/Views/Cart/_CartError.cshtml", new { Message = "No se pudo procesar su compra. Intenta nuevamente" });

            await _cartStore.ClearCartAsync();

            var CheckoutProViewModel = new CheckoutProViewModel
            {
                PreferenceId = result.PreferenceId,
                PublicKey = result.PublicKey
            };

            return PartialView("~/Presentation/Views/Checkout/_CheckoutWallet.cshtml", CheckoutProViewModel);
        }

        [HttpGet("checkout/return/{result}")]
        public async Task<IActionResult> Return([FromRoute] string result, [FromQuery] string? payment_id, [FromQuery] string? status)
        {
            var viewModel = new PaymentResultViewModel
            {
                Result = result,
                PaymentId = payment_id,
                Status = status
            };
            return View("~/Presentation/Views/Checkout/PaymentResult.cshtml", viewModel);
        }

        /// <summary>
        /// Inicia el flujo de pago con QR dinámico.
        ///
        /// Endpoint: POST /MercadoPago/PaymentQr
        /// Llamado desde: cart.js -> proceedToQr()
        ///
        /// Flujo:
        /// 1. Obtener items del carrito actual
        /// 2. Llamar al servicio para crear orden local y QR en MP
        /// 3. Vaciar el carrito (la orden ya fue creada)
        /// 4. Retornar vista parcial con la imagen QR
        ///
        /// La vista _Qr.cshtml muestra:
        /// - Imagen QR para escanear
        /// - Total a pagar
        /// - Estado de conexión SignalR
        /// - Botón para cancelar
        ///
        /// IMPORTANTE: Después de cargar la vista, el JavaScript (cart.js) debe
        /// inicializar SignalR manualmente porque los scripts dentro de innerHTML
        /// no se ejecutan automáticamente.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> PaymentQr()
        {
            // Obtener items del carrito actual
            var qrItems = await GetOrderItemsAsync();

            // Crear request para el servicio de pago QR
            var request = new StartQrRequest { Items = qrItems };

            // Llamar al servicio que:
            // 1. Crea la orden local
            // 2. Llama a la API de MP para crear el QR
            // 3. Genera la imagen QR en base64
            var result = await _paymentService.StartQrAsync(request);

            if (result is null)
                return PartialView("~/Presentation/Views/Cart/_CartError.cshtml", new { Message = "No se pudo generar el codigo QR. Intenta nuevamente" });

            // Vaciar el carrito ya que la orden fue creada exitosamente
            // El usuario ahora debe pagar escaneando el QR
            await _cartStore.ClearCartAsync();

            // Preparar ViewModel para la vista del QR
            var viewModel = new QrViewModel
            {
                OrderId = result.OrderId,           // ID local para SignalR
                QrImageBase64 = result.QrImageBase64, // Imagen QR lista para <img src="...">
                Total = result.Total,
                CurrencyId = result.CurrencyId,
                InStoreOrderId = result.InStoreOrderId // ID de MP (para debug)
            };

            // Retornar vista parcial que se carga en el offcanvas
            // El data-order-id en el HTML es usado por cart.js para inicializar SignalR
            return PartialView("~/Presentation/Views/Cart/_Qr.cshtml", viewModel);
        }
    }
}