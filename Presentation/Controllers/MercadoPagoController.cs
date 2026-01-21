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

        [HttpPost]
        public async Task<IActionResult> PaymentQr()
        {
            var qrItems = await GetOrderItemsAsync();

            var request = new StartQrRequest { Items = qrItems };

            var result = await _paymentService.StartQrAsync(request);

            if (result is null) return PartialView("~/Presentation/Views/Cart/_CartError.cshtml", new {Message = "No se pudo generar el codigo QR. Intenta nuevamente"});

            await _cartStore.ClearCartAsync();

            var viewModel = new QrViewModel
            {
                OrderId = result.OrderId,
                QrImageBase64 = result.QrImageBase64,
                Total = result.Total,
                CurrencyId = result.CurrencyId,
                InStoreOrderId = result.InStoreOrderId
            };

            return PartialView("~/Presentation/Views/Cart/_Qr.cshtml", viewModel);
        }
    }
}