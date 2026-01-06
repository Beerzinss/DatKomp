namespace DatKomp.Models;

public class SpecFilterGroup
{
    public string Key { get; set; } = string.Empty;
    public List<SpecFilterOption> Options { get; set; } = new();
}
