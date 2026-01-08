using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using DatKomp.Models;
using DatKomp.Services;

namespace DatKomp.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly ProductService _productService;
    private readonly MessageService _messageService;

    public HomeController(ILogger<HomeController> logger, ProductService productService, MessageService messageService)
    {
        _logger = logger;
        _productService = productService;
        _messageService = messageService;
    }

    public async Task<IActionResult> Index(string? category, List<string>? specFilters, string? q, int page = 1)
    {
        const int pageSize = 12;
        if (page < 1) page = 1;

        var qTrimmed = string.IsNullOrWhiteSpace(q) ? null : q.Trim();

        List<Product> products;

        if (string.IsNullOrWhiteSpace(category))
        {
            products = await _productService.GetAllProductsAsync();

            if (!string.IsNullOrWhiteSpace(qTrimmed))
            {
                products = products
                    .Where(p =>
                        (!string.IsNullOrWhiteSpace(p.Name) && p.Name.Contains(qTrimmed, StringComparison.OrdinalIgnoreCase))
                        || (!string.IsNullOrWhiteSpace(p.Description) && p.Description.Contains(qTrimmed, StringComparison.OrdinalIgnoreCase)))
                    .ToList();
            }

            var totalItems = products.Count;
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            if (totalPages > 0 && page > totalPages) page = totalPages;

            var pagedProducts = products
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var viewModelNoCategory = new ProductListViewModel
            {
                Products = pagedProducts,
                CurrentCategory = null,
                SearchQuery = qTrimmed,
                Page = page,
                PageSize = pageSize,
                TotalItems = totalItems
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

        // Apply filtering by specs:
        // - within the same spec key (e.g. "Cores"), selecting multiple values is OR (i5 OR i7)
        // - across different keys, it's AND (e.g. Cores=6 AND Socket=AM5)
        if (selectedPairs.Any())
        {
            var selectedByKey = selectedPairs
                .GroupBy(x => x.Key)
                .ToDictionary(g => g.Key, g => g.Select(x => x.Value).ToHashSet(StringComparer.Ordinal));

            products = products
                .Where(p =>
                    specsByProduct.TryGetValue(p.Id, out var specs)
                    && selectedByKey.All(group =>
                        specs.Any(s =>
                            string.Equals(s.SpecName, group.Key, StringComparison.Ordinal)
                            && s.SpecValue != null
                            && group.Value.Contains(s.SpecValue))))
                .ToList();
        }

        if (!string.IsNullOrWhiteSpace(qTrimmed))
        {
            products = products
                .Where(p =>
                    (!string.IsNullOrWhiteSpace(p.Name) && p.Name.Contains(qTrimmed, StringComparison.OrdinalIgnoreCase))
                    || (!string.IsNullOrWhiteSpace(p.Description) && p.Description.Contains(qTrimmed, StringComparison.OrdinalIgnoreCase)))
                .ToList();
        }

        var totalItemsFiltered = products.Count;
        var totalPagesFiltered = (int)Math.Ceiling(totalItemsFiltered / (double)pageSize);
        if (totalPagesFiltered > 0 && page > totalPagesFiltered) page = totalPagesFiltered;

        products = products
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        // Build spec filter groups from all specs for this category (not only filtered products)
        // Filter out null/empty spec keys/values (ProductSpec fields are optional in the form).
        var allSpecs = specsByProduct.Values
            .SelectMany(x => x)
            .Where(s => !string.IsNullOrWhiteSpace(s.SpecName) && !string.IsNullOrWhiteSpace(s.SpecValue));

        var groups = allSpecs
            .GroupBy(s => s.SpecName!)
            .OrderBy(g => g.Key)
            .Select(g => new SpecFilterGroup
            {
                Key = g.Key,
                Options = g
                    .GroupBy(s => new { SpecValue = s.SpecValue!, s.Unit })
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
            SearchQuery = qTrimmed,
            SpecFilters = groups,
            SelectedFilters = selectedFilters,
            Page = page,
            PageSize = pageSize,
            TotalItems = totalItemsFiltered
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

    [HttpGet]
    public IActionResult Contacts()
    {
        ViewBag.MessageSent = TempData["MessageSent"] as bool? == true;
        return View(new ContactMessageViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Contacts(ContactMessageViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var userId = 0;
        if (User.Identity?.IsAuthenticated == true)
        {
            var idClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (idClaim != null && int.TryParse(idClaim.Value, out var parsed))
            {
                userId = parsed;
            }
        }

        var message = new ContactMessage
        {
            UserId = userId,
            Email = null,
            Content = model.Text.Trim(),
            CreatedAtUtc = DateTime.UtcNow,
            IsRead = false
        };

        await _messageService.CreateMessageAsync(message);

        TempData["MessageSent"] = true;
        return RedirectToAction(nameof(Contacts));
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
