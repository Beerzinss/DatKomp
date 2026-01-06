namespace DatKomp.Models;

public class AdminOrderDetailsViewModel
{
    public OrderDetailsAdmin Order { get; set; } = new();
    public List<OrderStatusModel> Statuses { get; set; } = new();
    public int SelectedStatusId { get; set; }
}
