using System.ComponentModel.DataAnnotations;

namespace KopiKala.DTOs.Auth;

public class ResetPasswordRequestDto
{
    [Required(ErrorMessage = "Email wajib diisi.")]
    [EmailAddress(ErrorMessage = "Format email tidak valid.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Token reset kata sandi wajib diisi.")]
    public string Token { get; set; } = string.Empty;

    [Required(ErrorMessage = "Kata sandi baru wajib diisi.")]
    [MinLength(6, ErrorMessage = "Kata sandi minimal 6 karakter.")]
    public string NewPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Konfirmasi kata sandi baru wajib diisi.")]
    [Compare(nameof(NewPassword), ErrorMessage = "Konfirmasi kata sandi tidak cocok.")]
    public string ConfirmNewPassword { get; set; } = string.Empty;
}
