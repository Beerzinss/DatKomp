namespace DatKomp.Models;

public class UserOrderSummary
{
    public int Id { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public decimal ItemsTotal { get; set; }
    public decimal DeliveryPrice { get; set; }
    public decimal GrandTotal { get; set; }
    public string StatusName { get; set; } = string.Empty;
    public List<OrderItemSummary> Items { get; set; } = new();
}
