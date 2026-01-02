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

    public async Task<IActionResult> Index(string? category, List<string>? specFilters)
    {
        List<Product> products;

        if (string.IsNullOrWhiteSpace(category))
        {
            products = await _productService.GetAllProductsAsync();

            var viewModelNoCategory = new ProductListViewModel
            {
                Products = products,
                CurrentCategory = null
            };

            return View(viewModelNoCategory);
        }

        // With category: load products for that category and build spec filters
        products = await _productService.GetProductsByCategoryAsync(category);

        var productIds = products.Select(p => p.Id).ToList();
        var specsByProduct = await _productService.GetSpecsForProductsAsync(productIds);

        var selectedFilters = specFilters ?? new List<string>();

        // Parse selected filters into (key, value) pairs: format "key|value"
        var selectedPairs = selectedFilters
            .Select(f => f.Split('|', 2))
            .Where(parts => parts.Length == 2)
            .Select(parts => (Key: parts[0], Value: parts[1]))
            .ToList();

        // Apply filtering by specs: product must match all selected (key,value) pairs
        if (selectedPairs.Any())
        {
            products = products
                .Where(p => specsByProduct.TryGetValue(p.Id, out var specs)
                            && selectedPairs.All(sel =>
                                specs.Any(s => s.SpecName == sel.Key && s.SpecValue == sel.Value)))
                .ToList();
        }

        // Build spec filter groups from all specs for this category (not only filtered products)
        var allSpecs = specsByProduct.Values.SelectMany(x => x);

        var groups = allSpecs
            .GroupBy(s => s.SpecName)
            .OrderBy(g => g.Key)
            .Select(g => new SpecFilterGroup
            {
                Key = g.Key,
                Options = g
                    .GroupBy(s => new { s.SpecValue, s.Unit })
                    .OrderBy(x => x.Key.SpecValue)
                    .Select(x => new SpecFilterOption
                    {
                        Value = x.Key.SpecValue,
                        Unit = x.Key.Unit,
                        Selected = selectedPairs.Any(sel => sel.Key == g.Key && sel.Value == x.Key.SpecValue)
                    })
                    .ToList()
            })
            .ToList();

        var viewModel = new ProductListViewModel
        {
            Products = products,
            CurrentCategory = category,
            SpecFilters = groups,
            SelectedFilters = selectedFilters
        };

        ViewBag.CurrentCategory = category;
        return View(viewModel);
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
