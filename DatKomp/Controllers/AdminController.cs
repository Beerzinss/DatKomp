using DatKomp.Models;
using DatKomp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace DatKomp.Controllers;

[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    private readonly ILogger<AdminController> _logger;
    private readonly ProductService _productService;
    private readonly UserService _userService;
    private readonly OrderService _orderService;
    private readonly MessageService _messageService;

    public AdminController(
        ILogger<AdminController> logger,
        ProductService productService,
        UserService userService,
        OrderService orderService,
        MessageService messageService)
    {
        _logger = logger;
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
        _logger.LogInformation(
            "EditProduct POST: ProductId={ProductId}, Name={Name}, Price={Price}, StockQty={StockQty}",
            model.Product?.Id,
            model.Product?.Name,
            model.Product?.Price,
            model.Product?.StockQty);

        if (!ModelState.IsValid)
        {
            var errorMessages = ModelState
                .SelectMany(kvp => kvp.Value?.Errors.Select(e => $"{kvp.Key}: {e.ErrorMessage}") ?? Enumerable.Empty<string>())
                .ToArray();
            _logger.LogWarning("EditProduct POST ModelState invalid: {Errors}", string.Join(" | ", errorMessages));

            // The POST doesn't send AllCategories (and may send fewer spec rows), so rebuild the view model.
            model.AllCategories = await _productService.GetAllCategoriesAsync();
            model.SelectedCategoryIds ??= new List<int>();
            model.Specs ??= new List<ProductSpec>();
            while (model.Specs.Count < 5)
            {
                model.Specs.Add(new ProductSpec());
            }
            return View(model);
        }

        try
        {
            var product = model.Product;
            if (product == null)
            {
                throw new InvalidOperationException("Product payload is missing.");
            }

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

            TempData["AdminFlash"] = product.Id == 0
                ? $"Produkts pievienots (ID: {productId})."
                : "Produkts saglabāts.";

            return RedirectToAction(nameof(Products));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save product (Id={ProductId})", model.Product?.Id);
            ModelState.AddModelError(string.Empty, "Neizdevās saglabāt produktu. Pārbaudiet ievadītos datus un mēģiniet vēlreiz.");

            model.AllCategories = await _productService.GetAllCategoriesAsync();
            model.SelectedCategoryIds ??= new List<int>();
            model.Specs ??= new List<ProductSpec>();
            while (model.Specs.Count < 5)
            {
                model.Specs.Add(new ProductSpec());
            }

            return View(model);
        }
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

    [HttpGet]
    public async Task<IActionResult> EditUser(int? id)
    {
        if (id == null)
        {
            return View(new AdminUserEditViewModel());
        }

        var user = await _userService.GetByIdAsync(id.Value);
        if (user == null)
        {
            return NotFound();
        }

        var vm = new AdminUserEditViewModel
        {
            Id = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email,
            IsAdmin = user.IsAdmin
        };

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditUser(AdminUserEditViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var emailTrimmed = (model.Email ?? string.Empty).Trim();

        try
        {
            if (model.Id == 0)
            {
                if (string.IsNullOrWhiteSpace(model.Password))
                {
                    ModelState.AddModelError(nameof(model.Password), "Parole ir obligāta.");
                    return View(model);
                }

                if (model.Password != model.ConfirmPassword)
                {
                    ModelState.AddModelError(nameof(model.ConfirmPassword), "Paroles nesakrīt.");
                    return View(model);
                }

                var existing = await _userService.GetByEmailAsync(emailTrimmed);
                if (existing != null)
                {
                    ModelState.AddModelError(nameof(model.Email), "Lietotājs ar šo e-pastu jau eksistē.");
                    return View(model);
                }

                var newId = await _userService.CreateUserAsync(
                    model.FirstName,
                    model.LastName,
                    emailTrimmed,
                    model.Password,
                    model.IsAdmin);

                TempData["AdminFlash"] = $"Lietotājs pievienots (ID: {newId}).";
                return RedirectToAction(nameof(Users));
            }

            var current = await _userService.GetByIdAsync(model.Id);
            if (current == null)
            {
                TempData["AdminFlash"] = "Lietotājs netika atrasts.";
                return RedirectToAction(nameof(Users));
            }

            var currentUserIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (int.TryParse(currentUserIdString, out var currentUserId) && currentUserId == model.Id && !model.IsAdmin)
            {
                ModelState.AddModelError(nameof(model.IsAdmin), "Nevar noņemt admin tiesības pašreiz ielogotajam lietotājam.");
                return View(model);
            }

            var existingByEmail = await _userService.GetByEmailAsync(emailTrimmed);
            if (existingByEmail != null && existingByEmail.Id != model.Id)
            {
                ModelState.AddModelError(nameof(model.Email), "Lietotājs ar šo e-pastu jau eksistē.");
                return View(model);
            }

            var updatePayload = new AppUser
            {
                Id = model.Id,
                FirstName = model.FirstName,
                LastName = model.LastName,
                Email = emailTrimmed,
                IsAdmin = model.IsAdmin
            };

            var updated = await _userService.UpdateUserAsync(updatePayload);
            if (!updated)
            {
                TempData["AdminFlash"] = "Neizdevās saglabāt lietotāju.";
                return RedirectToAction(nameof(Users));
            }

            if (!string.IsNullOrWhiteSpace(model.Password))
            {
                if (model.Password != model.ConfirmPassword)
                {
                    ModelState.AddModelError(nameof(model.ConfirmPassword), "Paroles nesakrīt.");
                    return View(model);
                }

                var pwdUpdated = await _userService.UpdatePasswordAsync(model.Id, model.Password);
                if (!pwdUpdated)
                {
                    TempData["AdminFlash"] = "Lietotājs saglabāts, bet paroli neizdevās atjaunināt.";
                    return RedirectToAction(nameof(Users));
                }
            }

            TempData["AdminFlash"] = "Lietotājs saglabāts.";
            return RedirectToAction(nameof(Users));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save user (Id={UserId})", model.Id);
            ModelState.AddModelError(string.Empty, "Neizdevās saglabāt lietotāju.");
            return View(model);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteUser(int id)
    {
        var currentUserIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (int.TryParse(currentUserIdString, out var currentUserId) && currentUserId == id)
        {
            TempData["AdminFlash"] = "Nevar dzēst pašreiz ielogoto admin lietotāju.";
            return RedirectToAction(nameof(Users));
        }

        var user = await _userService.GetByIdAsync(id);
        if (user == null)
        {
            TempData["AdminFlash"] = "Lietotājs netika atrasts.";
            return RedirectToAction(nameof(Users));
        }

        if (user.IsAdmin)
        {
            var adminCount = await _userService.CountAdminsAsync();
            if (adminCount <= 1)
            {
                TempData["AdminFlash"] = "Nevar dzēst pēdējo admin lietotāju.";
                return RedirectToAction(nameof(Users));
            }
        }

        if (await _userService.UserHasOrdersAsync(id))
        {
            TempData["AdminFlash"] = "Lietotāju nevar dzēst, jo eksistē ar viņu saistīti pasūtījumi.";
            return RedirectToAction(nameof(Users));
        }

        try
        {
            var deleted = await _userService.DeleteUserAsync(id);
            TempData["AdminFlash"] = deleted
                ? "Lietotājs dzēsts."
                : "Neizdevās dzēst lietotāju.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete user (Id={UserId})", id);
            TempData["AdminFlash"] = "Neizdevās dzēst lietotāju.";
        }

        return RedirectToAction(nameof(Users));
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

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteDeliveryType(int id)
    {
        if (await _orderService.DeliveryTypeHasOrdersAsync(id))
        {
            TempData["AdminFlash"] = "Piegādes veidu nevar dzēst, jo tas ir izmantots pasūtījumos.";
            return RedirectToAction(nameof(DeliveryTypes));
        }

        try
        {
            var deleted = await _orderService.DeleteDeliveryTypeAsync(id);
            TempData["AdminFlash"] = deleted
                ? "Piegādes veids dzēsts."
                : "Neizdevās dzēst piegādes veidu.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete delivery type (Id={DeliveryTypeId})", id);
            TempData["AdminFlash"] = "Neizdevās dzēst piegādes veidu.";
        }

        return RedirectToAction(nameof(DeliveryTypes));
    }
}
