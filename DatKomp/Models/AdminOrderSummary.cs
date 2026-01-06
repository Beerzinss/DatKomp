namespace DatKomp.Models;

public class AdminOrderSummary
{
    public int Id { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public decimal ItemsTotal { get; set; }
    public decimal DeliveryPrice { get; set; }
    public decimal GrandTotal { get; set; }
    public int StatusId { get; set; }
    public string StatusName { get; set; } = string.Empty;
}
