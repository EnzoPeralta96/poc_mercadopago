using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using poc_mercadopago.Models;
using poc_mercadopago.Presentation.ViewModels;
using poc_mercadopago.Repository.ProductRepository;

namespace poc_mercadopago.Controllers;

public class HomeController : Controller
{
    private readonly IProductRepository _productRepository;
    public HomeController(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    public async Task<IActionResult> Index()
    {
        var products = await _productRepository.GetAllAsync();
        
        var productListItems = products.Select(p =>
                                    new ProductListItemViewModel(
                                        p.Id, p.Name,
                                        p.Description,
                                        p.Price, p.CurrencyId
                                )).ToList();
                                
        var indexViewModel = new HomeIndexViewModel
        {
            Products = productListItems
        };

        return View(indexViewModel);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
