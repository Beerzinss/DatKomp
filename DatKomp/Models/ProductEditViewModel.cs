namespace DatKomp.Models;

public class ProductEditViewModel
{
    public Product Product { get; set; } = new();
    public List<Category> AllCategories { get; set; } = new();
    public List<int> SelectedCategoryIds { get; set; } = new();
    public List<ProductSpec> Specs { get; set; } = new();
}
