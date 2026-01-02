namespace DatKomp.Models;

public class ProductListViewModel
{
    public List<Product> Products { get; set; } = new();
    public string? CurrentCategory { get; set; }
    public List<SpecFilterGroup> SpecFilters { get; set; } = new();
    public List<string> SelectedFilters { get; set; } = new();
}
