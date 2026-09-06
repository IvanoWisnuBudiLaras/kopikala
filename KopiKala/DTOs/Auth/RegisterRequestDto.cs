using System.ComponentModel.DataAnnotations;

namespace KopiKala.DTOs.Auth;

public class RegisterRequestDto
{
    [Required(ErrorMessage = "Nama lengkap wajib diisi.")]
    [StringLength(100, ErrorMessage = "Nama maksimal 100 karakter.")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email wajib diisi.")]
    [EmailAddress(ErrorMessage = "Format email tidak valid.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Nomor WhatsApp wajib diisi.")]
    [Phone(ErrorMessage = "Format nomor telepon tidak valid.")]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "Kata sandi wajib diisi.")]
    [MinLength(6, ErrorMessage = "Kata sandi minimal 6 karakter.")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Konfirmasi kata sandi wajib diisi.")]
    [Compare(nameof(Password), ErrorMessage = "Konfirmasi kata sandi tidak cocok.")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
