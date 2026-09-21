using System.ComponentModel.DataAnnotations;

namespace AbuAdelERP.Api.DTOs;

public class LoginDto
{
    [Required(ErrorMessage = "اسم المستخدم مطلوب")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "كلمة المرور مطلوبة")]
    public string Password { get; set; } = string.Empty;
}