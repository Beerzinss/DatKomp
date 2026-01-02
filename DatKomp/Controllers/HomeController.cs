using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using DatKomp.Models;
using DatKomp.Services;

namespace DatKomp.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly ProductService _productService;

    public HomeController(ILogger<HomeController> logger, ProductService productService)
    {
        _logger = logger;
        _productService = productService;
    }

    public async Task<IActionResult> Index()
    {
        var products = await _productService.GetAllProductsAsync();
        return View(products);
    }

    public IActionResult About()
    {
        return View();
    }

    public IActionResult Service()
    {
        return View();
    }

    public IActionResult Contacts()
    {
        return View();
    }

    public async Task<IActionResult> Details(int id)
    {
        var product = await _productService.GetProductByIdAsync(id);

        if (product == null)
        {
            return NotFound();
        }

        var specs = await _productService.GetProductSpecsByProductIdAsync(id);

        var viewModel = new ProductDetailsViewModel
        {
            Product = product,
            Specs = specs
        };

        return View(viewModel);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
