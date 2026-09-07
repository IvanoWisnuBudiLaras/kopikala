using System.ComponentModel.DataAnnotations;

namespace KopiKala.DTOs.Staff;

public class ExtendDurationRequestDto
{
    [Required]
    public Guid BookingId { get; set; }

    [Range(1, 2, ErrorMessage = "Penambahan durasi antara 1 sampai 2 jam.")]
    public int AdditionalHours { get; set; } = 1;
}
