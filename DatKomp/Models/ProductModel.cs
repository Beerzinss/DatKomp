using System.ComponentModel.DataAnnotations.Schema;

namespace DatKomp.Models;

[Table("product")]
public class Product
{
    [Column("id")]
    public int Id { get; set; }

    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Column("description")]
    public string? Description { get; set; }

    [Column("price")]
    public decimal Price { get; set; }

    [Column("stock_qty")]
    public int StockQty { get; set; }

    [Column("image_url")]
    public string? ImageUrl { get; set; }
}
