using System.ComponentModel.DataAnnotations;
using KopiKala.DTOs.Booking;

namespace KopiKala.DTOs.Staff;

public class WalkInBookingRequestDto
{
    [Required(ErrorMessage = "Meja wajib dipilih.")]
    public Guid TableId { get; set; }

    [Required(ErrorMessage = "Sesi jam wajib dipilih.")]
    public int TimeslotId { get; set; }

    [Range(1, 3, ErrorMessage = "Durasi duduk 1-3 jam.")]
    public int DurationHours { get; set; } = 2;

    [Required(ErrorMessage = "Nama perwakilan tamu wajib diisi.")]
    [StringLength(100, ErrorMessage = "Nama maksimal 100 karakter.")]
    public string RepresentativeName { get; set; } = string.Empty;

    public string? CustomerPhone { get; set; }

    [Required(ErrorMessage = "Metode pembayaran wajib dipilih.")]
    public string PaymentMethod { get; set; } = "Tunai"; // "Tunai", "QRIS", "BayarDiTempat"

    public List<OrderItemDto> SelectedItems { get; set; } = new();
}
