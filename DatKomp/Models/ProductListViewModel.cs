namespace DatKomp.Models;

public class ProductListViewModel
{
    public List<Product> Products { get; set; } = new();
    public string? CurrentCategory { get; set; }
    public List<SpecFilterGroup> SpecFilters { get; set; } = new();
    public List<string> SelectedFilters { get; set; } = new();

    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 12;
    public int TotalItems { get; set; }
    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalItems / (double)PageSize);
}
