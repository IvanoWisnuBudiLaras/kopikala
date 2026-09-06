using System.ComponentModel.DataAnnotations;

namespace KopiKala.DTOs.Auth;

public class LoginRequestDto
{
    [Required(ErrorMessage = "Email wajib diisi.")]
    [EmailAddress(ErrorMessage = "Format email tidak valid.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Kata sandi wajib diisi.")]
    public string Password { get; set; } = string.Empty;

    public bool RememberMe { get; set; }
}
