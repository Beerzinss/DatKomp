using System.ComponentModel.DataAnnotations.Schema;

namespace DatKomp.Models;

[Table("contact_message")]
public class ContactMessage
{
    [Column("id")]
    public int Id { get; set; }

    [Column("user_id")]
    public int UserId { get; set; }

    [Column("email")]
    public string? Email { get; set; }

    [Column("content")]
    public string Content { get; set; } = string.Empty;

    [Column("created_at_utc")]
    public DateTime CreatedAtUtc { get; set; }

    [Column("is_read")]
    public bool IsRead { get; set; }
}
