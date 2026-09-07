using System.ComponentModel.DataAnnotations;
using KopiKala.DTOs.Booking;

namespace KopiKala.DTOs.Staff;

public class AddOnOrderRequestDto
{
    [Required]
    public Guid BookingId { get; set; }

    [Required]
    public List<OrderItemDto> Items { get; set; } = new();
}
