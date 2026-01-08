using System.ComponentModel.DataAnnotations;

namespace DatKomp.Models;

public class AdminUserEditViewModel
{
    public int Id { get; set; }

    [Required]
    [Display(Name = "Vārds")]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Uzvārds")]
    public string LastName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [Display(Name = "E-pasts")]
    public string Email { get; set; } = string.Empty;

    [Display(Name = "Admin")]
    public bool IsAdmin { get; set; }

    [DataType(DataType.Password)]
    [Display(Name = "Parole")]
    public string? Password { get; set; }

    [DataType(DataType.Password)]
    [Display(Name = "Apstipriniet paroli")]
    public string? ConfirmPassword { get; set; }
}
