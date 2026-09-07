using System.ComponentModel.DataAnnotations;

namespace KopiKala.DTOs.Booking;

public class OrderItemDto
{
    [Required]
    public Guid MenuItemId { get; set; }

    public string Name { get; set; } = string.Empty;

    [Range(1, 100, ErrorMessage = "Jumlah pesanan minimal 1 porsi")]
    public int Quantity { get; set; } = 1;

    public decimal UnitPrice { get; set; }

    public decimal SubTotal => Quantity * UnitPrice;
}
