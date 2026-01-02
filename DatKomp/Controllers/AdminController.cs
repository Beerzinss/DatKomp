using DatKomp.Models;
using DatKomp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DatKomp.Controllers;

[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    private readonly ProductService _productService;
    private readonly UserService _userService;
    private readonly OrderService _orderService;
    private readonly MessageService _messageService;

    public AdminController(ProductService productService, UserService userService, OrderService orderService, MessageService messageService)
    {
        _productService = productService;
        _userService = userService;
        _orderService = orderService;
        _messageService = messageService;
    }

    public IActionResult Index()
    {
        return View();
    }

    // Messages
    public async Task<IActionResult> Messages()
    {
        var messages = await _messageService.GetAllMessagesAsync();
        return View(messages);
    }

    // Products
    public async Task<IActionResult> Products()
    {
        var products = await _productService.GetAllProductsAsync();
        return View(products);
    }

    [HttpGet]
    public async Task<IActionResult> EditProduct(int? id)
    {
        var categories = await _productService.GetAllCategoriesAsync();

        if (id == null)
        {
            var vmNew = new ProductEditViewModel
            {
                Product = new Product(),
                AllCategories = categories,
                SelectedCategoryIds = new List<int>(),
                Specs = new List<ProductSpec>()
            };

            // add a few empty spec rows for convenience
            while (vmNew.Specs.Count < 5)
            {
                vmNew.Specs.Add(new ProductSpec());
            }

            return View(vmNew);
        }

        var product = await _productService.GetProductByIdAsync(id.Value);
        if (product == null)
        {
            return NotFound();
        }
        var selectedCategoryIds = await _productService.GetCategoryIdsForProductAsync(id.Value);
        var specs = await _productService.GetProductSpecsByProductIdAsync(id.Value);

        while (specs.Count < 5)
        {
            specs.Add(new ProductSpec());
        }

        var vm = new ProductEditViewModel
        {
            Product = product,
            AllCategories = categories,
            SelectedCategoryIds = selectedCategoryIds,
            Specs = specs
        };

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditProduct(ProductEditViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var product = model.Product;
        int productId;

        if (product.Id == 0)
        {
            productId = await _productService.CreateProductAsync(product);
        }
        else
        {
            await _productService.UpdateProductAsync(product);
            productId = product.Id;
        }

        await _productService.ReplaceProductCategoriesAsync(productId, model.SelectedCategoryIds ?? new List<int>());
        await _productService.ReplaceProductSpecsAsync(productId, model.Specs ?? new List<ProductSpec>());

        return RedirectToAction(nameof(Products));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteProduct(int id)
    {
        await _productService.DeleteProductAsync(id);
        return RedirectToAction(nameof(Products));
    }

    // Users
    public async Task<IActionResult> Users()
    {
        var users = await _userService.GetAllUsersAsync();
        return View(users);
    }

    // Orders
    public async Task<IActionResult> Orders()
    {
        var orders = await _orderService.GetAllOrdersAsync();
        return View(orders);
    }

    public async Task<IActionResult> OrderDetails(int id)
    {
        var order = await _orderService.GetOrderDetailsAsync(id);
        if (order == null)
        {
            return NotFound();
        }

        var statuses = await _orderService.GetAllOrderStatusesAsync();

        var vm = new AdminOrderDetailsViewModel
        {
            Order = order,
            Statuses = statuses,
            SelectedStatusId = order.StatusId
        };

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateOrderStatus(AdminOrderDetailsViewModel model)
    {
        await _orderService.UpdateOrderStatusAsync(model.Order.Id, model.SelectedStatusId);
        return RedirectToAction(nameof(OrderDetails), new { id = model.Order.Id });
    }

    // Delivery types
    public async Task<IActionResult> DeliveryTypes()
    {
        var types = await _orderService.GetAllDeliveryTypesAsync();
        return View(types);
    }

    [HttpGet]
    public async Task<IActionResult> EditDeliveryType(int? id)
    {
        if (id == null)
        {
            return View(new DeliveryType { IsActive = true });
        }

        var all = await _orderService.GetAllDeliveryTypesAsync();
        var type = all.FirstOrDefault(t => t.Id == id.Value);
        if (type == null)
        {
            return NotFound();
        }

        return View(type);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditDeliveryType(DeliveryType model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        if (model.Id == 0)
        {
            await _orderService.CreateDeliveryTypeAsync(model);
        }
        else
        {
            await _orderService.UpdateDeliveryTypeAsync(model);
        }

        return RedirectToAction(nameof(DeliveryTypes));
    }
}
