using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using poc_mercadopago.Application.DTOs.StartCheckoutDTO;
using poc_mercadopago.Application.Services.PaymentService;
using poc_mercadopago.Infrastructure.Cart.CartStore;
using poc_mercadopago.Presentation.ViewModels.CheckoutViewModels;

namespace poc_mercadopago.Controllers
{
    public class CheckoutController : Controller
    {
        private readonly IPaymentService _paymentService;
        private readonly ICartStore _cartStore;
        private readonly ILogger<CheckoutController> _logger;

        public CheckoutController(ILogger<CheckoutController> logger, IPaymentService paymentService, ICartStore cartStore)
        {
            _logger = logger;
            _paymentService = paymentService;
            _cartStore = cartStore;
        }

        private async Task<List<CheckoutCartItemDTO>> GetCheckoutItemsAsync()
        {
            var cart = await _cartStore.GetCartAsync();
            if (!cart.Items.Any()) return [];

            return cart.Items.Select(item => new CheckoutCartItemDTO
            {
                ProductId = item.ProductId,
                Quantity = item.Quantity
            }).ToList();
        }

        [HttpPost]
        public async Task<IActionResult> Checkout()
        {
            var checkoutItems = await GetCheckoutItemsAsync();
            // Validar que no esté vacío
            if (!checkoutItems.Any()) return PartialView("_CartEmpty");

            var request = new StartCheckoutRequest
            {
                Items = checkoutItems
            };

            var result = await _paymentService.StartCheckoutAsync(request);

            if (result is null)
                return PartialView("_CartError", new { Message = "No se pudo procesar su compra. Intenta nuevamente" });

            await _cartStore.ClearCartAsync();

            var CheckoutProViewModel = new CheckoutProViewModel
            {
                PreferenceId = result.PreferenceId,
                PublicKey = result.PublicKey
            };

            return PartialView("_CheckoutWallet", CheckoutProViewModel);
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
            return View("PaymentResult", viewModel);
        }
    }
}