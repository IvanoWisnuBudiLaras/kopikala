using System.ComponentModel.DataAnnotations;

namespace KopiKala.DTOs.Booking;

public class CreateBookingRequestDto
{
    [Required(ErrorMessage = "Pilih meja terlebih dahulu")]
    public Guid TableId { get; set; }

    [Required(ErrorMessage = "Pilih tanggal reservasi")]
    public DateOnly BookingDate { get; set; }

    [Required(ErrorMessage = "Pilih sesi waktu")]
    public int TimeslotId { get; set; }

    [Range(1, 3, ErrorMessage = "Durasi duduk harus antara 1 sampai 3 jam")]
    public int DurationHours { get; set; } = 2;

    [Required(ErrorMessage = "Nama perwakilan wajib diisi")]
    [StringLength(100, ErrorMessage = "Nama perwakilan maksimal 100 karakter")]
    public string RepresentativeName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Nomor WhatsApp wajib diisi")]
    [Phone(ErrorMessage = "Format nomor WhatsApp tidak valid")]
    public string CustomerPhone { get; set; } = string.Empty;

    [Required(ErrorMessage = "Pilih metode pembayaran")]
    public string PaymentMethod { get; set; } = "TransferBank"; // 'TransferBank' or 'BayarDiTempat'

    public List<OrderItemDto> SelectedItems { get; set; } = new();
}
