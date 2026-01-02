using System.ComponentModel.DataAnnotations;

namespace DatKomp.Models;

public class LoginViewModel
{
    [Required]
    [EmailAddress]
    [Display(Name = "E-pasts")]
    public string Email { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    [Display(Name = "Parole")]
    public string Password { get; set; } = string.Empty;
}
