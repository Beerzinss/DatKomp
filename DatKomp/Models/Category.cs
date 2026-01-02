using System.ComponentModel.DataAnnotations.Schema;

namespace DatKomp.Models;

[Table("category")]
public class Category
{
    [Column("id")]
    public int Id { get; set; }

    [Column("name")]
    public string Name { get; set; } = string.Empty;
}
