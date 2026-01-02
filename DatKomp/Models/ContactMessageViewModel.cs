using System.ComponentModel.DataAnnotations;

namespace DatKomp.Models;

public class ContactMessageViewModel
{
    [Required]
    [StringLength(2000)]
    public string Text { get; set; } = string.Empty;
}
