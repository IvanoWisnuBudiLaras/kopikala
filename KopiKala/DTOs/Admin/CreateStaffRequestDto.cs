using System.ComponentModel.DataAnnotations;

namespace KopiKala.DTOs.Admin;

public class CreateStaffRequestDto
{
    [Required(ErrorMessage = "Nama lengkap wajib diisi.")]
    [StringLength(100, ErrorMessage = "Nama lengkap maksimal 100 karakter.")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email wajib diisi.")]
    [EmailAddress(ErrorMessage = "Format email tidak valid.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Nomor WhatsApp wajib diisi.")]
    [Phone(ErrorMessage = "Format nomor telepon tidak valid.")]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password wajib diisi.")]
    [MinLength(6, ErrorMessage = "Password minimal 6 karakter.")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Peran wajib dipilih.")]
    public int RoleId { get; set; }
}
