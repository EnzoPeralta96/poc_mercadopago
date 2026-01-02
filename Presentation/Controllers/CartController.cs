using Microsoft.AspNetCore.Mvc;
using poc_mercadopago.Infrastructure.Cart.CartStore;
using poc_mercadopago.Infrastructure.Cart.DTOs;
using poc_mercadopago.Presentation.ViewModels.CartViewModels;
using poc_mercadopago.Repository.ProductRepository;


namespace poc_mercadopago.Controllers;

public class CartController : Controller
{
    private readonly ILogger<CartController> _logger;
    private readonly ICartStore _cartStore;
    private readonly IProductRepository _productRepository;
    public CartController(ICartStore cartStore, IProductRepository productRepository, ILogger<CartController> logger)
    {
        _cartStore = cartStore;
        _productRepository = productRepository;
        _logger = logger;
    }

    private async Task<CartViewModel> EnrichCartAsync()
    {
        var cartView = new CartViewModel();

        var cart = await _cartStore.GetCartAsync();
        if (!cart.Items.Any()) return cartView;

        //Lista de id de productos selecionados
        var ids = cart.Items.Select(i => i.ProductId).ToList();

        //Productos selecionados
        var products = await _productRepository.GetByIdsAsync(ids);

        //Por cada item del carrito creo el CartItem para mostrar en el carrito.
        foreach (var item in cart.Items)
        {
            var product = await _productRepository.GetByIdAsync(item.ProductId);
            var cartItemView = new CartItemViewModel
            {
                ProductId = item.ProductId,
                ProductName = product.Name,
                ProductDescription = product.Description,
                UnitPrice = product.Price,
                Quantity = item.Quantity,
                CurrencyId = product.CurrencyId
            };
            cartView.Items.Add(cartItemView);
        }
        return cartView;
    }

    public async Task<IActionResult> GetCartPartial()
    {
        var cartView = await EnrichCartAsync();
        return PartialView("_CartOffcanvas", cartView);
    }

    [HttpPost]
    public async Task<IActionResult> AddToCart(CartRequestViewModel request)
    {
        if (!ModelState.IsValid)
        {
            _logger.LogWarning("Validacion fallida al agregar al carrito");
            return PartialView("_CartError", new { Message = "Datos inválidos. Intenta nuevamente" });

        }

        var cart = await _cartStore.GetCartAsync();

        var existingItemInCart = cart.Items.FirstOrDefault(i => i.ProductId == request.ProductId);

        if (existingItemInCart is null)
        {
            cart.Items.Add(new CartItemDTO
            {
                ProductId = request.ProductId,
                Quantity = request.Quantity
            });
        }
        else
        {
            existingItemInCart.Quantity += request.Quantity;
        }

        await _cartStore.SaveCartAsync(cart);

        var viewModel = await EnrichCartAsync();
        return PartialView("_CartOffcanvas", viewModel);
    }

    [HttpPost]
    public async Task<IActionResult> UpdateCart(CartRequestViewModel request)
    {
        if (!ModelState.IsValid)
        {
            _logger.LogWarning("Validacion fallida al modificar el carrito");
            return PartialView("_CartError", new { Message = "Datos inválidos. Intenta nuevamente" });
        }

        var cart = await _cartStore.GetCartAsync();
        var item = cart.Items.FirstOrDefault(i => i.ProductId == request.ProductId);

        if (item is not null)
        {
            if (request.Quantity <= 0)
            {
                cart.Items.Remove(item);
            }
            else
            {
                item.Quantity = request.Quantity;
            }
            await _cartStore.SaveCartAsync(cart);
        }

        var viewModel = await EnrichCartAsync();
        return PartialView("_CartOffcanvas", viewModel);
    }

    [HttpPost]
    public async Task<IActionResult> RemoveFromCart(string productId)
    {
        if (string.IsNullOrEmpty(productId) || string.IsNullOrWhiteSpace(productId))
        {
            _logger.LogWarning("Validacion fallida al modificar el carrito");
            return PartialView("_CartError", new { Message = "Datos inválidos. Intenta nuevamente" });
        }

        var cart = await _cartStore.GetCartAsync();
        var item = cart.Items.FirstOrDefault(i => i.ProductId == productId);

        if (item is not null)
        {
            cart.Items.Remove(item);
            await _cartStore.SaveCartAsync(cart);
        }

        var viewModel = await EnrichCartAsync();
        return PartialView("_CartOffcanvas", viewModel);
    }

    [HttpPost]
    public async Task<IActionResult> ClearCart()
    {
        await _cartStore.ClearCartAsync();
        var viewModel = await EnrichCartAsync();
        return PartialView("_CartOffcanvas", viewModel);
    }


}
