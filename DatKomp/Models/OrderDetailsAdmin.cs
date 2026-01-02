namespace DatKomp.Models;

public class OrderDetailsAdmin
{
    public int Id { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string AddressLine { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string DeliveryTypeName { get; set; } = string.Empty;
    public decimal ItemsTotal { get; set; }
    public decimal DeliveryPrice { get; set; }
    public decimal GrandTotal { get; set; }
    public int StatusId { get; set; }
    public string StatusName { get; set; } = string.Empty;
    public List<OrderItemSummary> Items { get; set; } = new();
}
