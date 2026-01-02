using System.ComponentModel.DataAnnotations.Schema;

namespace DatKomp.Models;

[Table("product_spec")]
public class ProductSpec
{
    [Column("id")]
    public int Id { get; set; }

    [Column("product_id")]
    public int ProductId { get; set; }

    [Column("key")]
    public string SpecName { get; set; } = string.Empty;

    [Column("value")]
    public string SpecValue { get; set; } = string.Empty;

    [Column("unit")]
    public string? Unit { get; set; }
}
