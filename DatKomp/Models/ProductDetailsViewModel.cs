namespace DatKomp.Models;

public class ProductDetailsViewModel
{
    public Product Product { get; set; } = null!;

    public List<ProductSpec> Specs { get; set; } = new();
}
