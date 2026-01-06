using System.ComponentModel.DataAnnotations;

namespace DatKomp.Models;

public class RegisterViewModel
{
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

    [Required]
    [DataType(DataType.Password)]
    [Display(Name = "Parole")]
    public string Password { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    [Display(Name = "Apstipriniet paroli")]
    [Compare("Password", ErrorMessage = "Paroles nesakrīt.")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
