using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AbuAdelERP.Api.Models;

[Table("users", Schema = "erp")]
public class AppUser
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Required]
    [Column("username")]
    public string Username { get; set; } = string.Empty;

    [Required]
    [Column("password_hash")]
    public string PasswordHash { get; set; } = string.Empty;

    [Required]
    [Column("role")]
    public string Role { get; set; } = "User";
}