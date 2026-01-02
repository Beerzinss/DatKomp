using System.Text.Json;
using DatKomp.Models;
using DatKomp.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DatKomp.Controllers;

public class CartController : Controller
{
    private const string CartSessionKey = "Cart";
    private readonly ProductService _productService;
    private readonly OrderService _orderService;

    public CartController(ProductService productService, OrderService orderService)
    {
        _productService = productService;
        _orderService = orderService;
    }

    [HttpPost]
    public async Task<IActionResult> Add(int productId)
    {
        var product = await _productService.GetProductByIdAsync(productId);
        if (product == null)
        {
            return NotFound();
        }

        var cart = GetCart();
        var existing = cart.FirstOrDefault(c => c.ProductId == productId);
        if (existing != null)
        {
            existing.Quantity++;
        }
        else
        {
            cart.Add(new CartItem
            {
                ProductId = product.Id,
                Name = product.Name,
                Price = product.Price,
                Quantity = 1,
                ImageUrl = product.ImageUrl
            });
        }

        SaveCart(cart);

        return RedirectToAction("Details", "Home", new { id = productId });
    }

    public IActionResult Index()
    {
        var cart = GetCart();
        return View(cart);
    }

    [HttpGet]
    public async Task<IActionResult> Checkout()
    {
        var cart = GetCart();
        if (!cart.Any())
        {
            return RedirectToAction("Index");
        }

        var deliveryTypes = await _orderService.GetActiveDeliveryTypesAsync();

        var model = new CheckoutViewModel
        {
            CartItems = cart,
            DeliveryTypes = deliveryTypes
        };

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Checkout(CheckoutViewModel model)
    {
        var cart = GetCart();
        if (!cart.Any())
        {
            ModelState.AddModelError(string.Empty, "Grozs ir tukšs.");
        }

        if (!ModelState.IsValid)
        {
            model.CartItems = cart;
            model.DeliveryTypes = await _orderService.GetActiveDeliveryTypesAsync();
            return View(model);
        }

        var userId = 0;
        if (User.Identity?.IsAuthenticated == true)
        {
            var idClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (idClaim != null && int.TryParse(idClaim.Value, out var parsedId))
            {
                userId = parsedId;
            }
        }

        var defaultOrderStatusId = 1; // assumes status id 1 = 'New' or similar

        var orderId = await _orderService.CreateOrderAsync(model, cart, userId, defaultOrderStatusId);

        // Clear cart after successful order
        SaveCart(new List<CartItem>());

        return RedirectToAction("Confirmation", new { id = orderId });
    }

    public IActionResult Confirmation(int id)
    {
        ViewBag.OrderId = id;
        return View();
    }

    [HttpPost]
    public IActionResult Increment(int productId)
    {
        var cart = GetCart();
        var existing = cart.FirstOrDefault(c => c.ProductId == productId);
        if (existing != null)
        {
            existing.Quantity++;
            SaveCart(cart);
        }

        return RedirectToAction("Index");
    }

    [HttpPost]
    public IActionResult Decrement(int productId)
    {
        var cart = GetCart();
        var existing = cart.FirstOrDefault(c => c.ProductId == productId);
        if (existing != null)
        {
            existing.Quantity--;
            if (existing.Quantity <= 0)
            {
                cart.Remove(existing);
            }

            SaveCart(cart);
        }

        return RedirectToAction("Index");
    }

    [HttpPost]
    public IActionResult Remove(int productId)
    {
        var cart = GetCart();

        var existing = cart.FirstOrDefault(c => c.ProductId == productId);
        if (existing != null)
        {
            cart.Remove(existing);
            SaveCart(cart);
        }

        return RedirectToAction("Index");
    }

    private List<CartItem> GetCart()
    {
        var json = HttpContext.Session.GetString(CartSessionKey);
        if (string.IsNullOrEmpty(json))
        {
            return new List<CartItem>();
        }

        return JsonSerializer.Deserialize<List<CartItem>>(json) ?? new List<CartItem>();
    }

    private void SaveCart(List<CartItem> cart)
    {
        var json = JsonSerializer.Serialize(cart);
        HttpContext.Session.SetString(CartSessionKey, json);
    }
}
