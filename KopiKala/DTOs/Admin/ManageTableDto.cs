using System.ComponentModel.DataAnnotations;

namespace KopiKala.DTOs.Admin;

public class ManageTableDto
{
    public Guid? TableId { get; set; }

    [Required(ErrorMessage = "Nomor meja wajib diisi.")]
    [StringLength(10, ErrorMessage = "Nomor meja maksimal 10 karakter.")]
    public string TableNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "Kapasitas kursi wajib diisi.")]
    [Range(1, 20, ErrorMessage = "Kapasitas meja harus antara 1 sampai 20 orang.")]
    public int Capacity { get; set; } = 4;

    [Required(ErrorMessage = "Area meja wajib dipilih.")]
    [StringLength(20, ErrorMessage = "Area meja maksimal 20 karakter.")]
    public string Area { get; set; } = "Indoor AC";

    public bool IsActive { get; set; } = true;
}
