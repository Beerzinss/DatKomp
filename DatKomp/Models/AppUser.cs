using System.ComponentModel.DataAnnotations.Schema;

namespace DatKomp.Models;

[Table("app_user")]
public class AppUser
{
    [Column("id")]
    public int Id { get; set; }

    [Column("first_name")]
    public string FirstName { get; set; } = string.Empty;

    [Column("last_name")]
    public string LastName { get; set; } = string.Empty;

    [Column("email")]
    public string Email { get; set; } = string.Empty;

    [Column("password_hash")]
    public string PasswordHash { get; set; } = string.Empty;

    [Column("is_admin")]
    public bool IsAdmin { get; set; }
}
