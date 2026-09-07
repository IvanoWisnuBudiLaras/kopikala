using System.ComponentModel.DataAnnotations;

namespace KopiKala.DTOs.Staff;

public class SubstituteItemRequestDto
{
    [Required]
    public Guid BookingId { get; set; }

    [Required]
    public Guid OldDetailId { get; set; }

    [Required]
    public Guid NewMenuItemId { get; set; }

    [Range(1, 99, ErrorMessage = "Jumlah item minimal 1.")]
    public int NewQuantity { get; set; } = 1;
}
