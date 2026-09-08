using System.ComponentModel.DataAnnotations;

namespace KopiKala.DTOs.Admin;

public class ManageMenuItemDto
{
    public Guid? MenuItemId { get; set; }

    [Required(ErrorMessage = "Nama menu wajib diisi.")]
    [StringLength(100, ErrorMessage = "Nama menu maksimal 100 karakter.")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Kategori menu wajib dipilih.")]
    [StringLength(20, ErrorMessage = "Kategori maksimal 20 karakter.")]
    public string Category { get; set; } = "Coffee";

    [Required(ErrorMessage = "Harga menu wajib diisi.")]
    [Range(0, 10000000, ErrorMessage = "Harga menu harus lebih dari Rp 0.")]
    public decimal Price { get; set; } = 20000;

    [Required(ErrorMessage = "Stok awal wajib diisi.")]
    [Range(0, 10000, ErrorMessage = "Stok tidak boleh negatif.")]
    public int Stock { get; set; } = 50;

    public string? ImageUrl { get; set; }

    public bool IsAvailable { get; set; } = true;
}
