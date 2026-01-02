using System.ComponentModel.DataAnnotations.Schema;

namespace DatKomp.Models;

[Table("delivery_type")]
public class DeliveryType
{
    [Column("id")]
    public int Id { get; set; }

    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Column("description")]
    public string? Description { get; set; }

    [Column("price")]
    public decimal Price { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; }
}
