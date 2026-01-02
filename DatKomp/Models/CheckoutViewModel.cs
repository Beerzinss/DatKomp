using System.ComponentModel.DataAnnotations;

namespace DatKomp.Models;

public class CheckoutViewModel
{
    // Customer info
    [Required]
    [Display(Name = "Vārds")]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Uzvārds")]
    public string LastName { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Telefona nr.")]
    public string Phone { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [Display(Name = "E-pasts")]
    public string Email { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Adrese")]
    public string AddressLine { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Piegādes veids")]
    public int DeliveryTypeId { get; set; }

    // Selection lists
    public List<DeliveryType> DeliveryTypes { get; set; } = new();

    // Cart summary
    public List<CartItem> CartItems { get; set; } = new();

    public decimal ItemsTotal => CartItems.Sum(i => i.Price * i.Quantity);

    public decimal DeliveryPrice { get; set; }

    public decimal GrandTotal => ItemsTotal + DeliveryPrice;
}
