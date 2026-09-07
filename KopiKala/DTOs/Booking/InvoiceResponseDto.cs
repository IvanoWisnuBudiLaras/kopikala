namespace KopiKala.DTOs.Booking;

public class InvoiceResponseDto
{
    public Guid BookingId { get; set; }
    public string InvoiceCode { get; set; } = string.Empty;
    public string TableNumber { get; set; } = string.Empty;
    public string Area { get; set; } = string.Empty;
    public int Capacity { get; set; }
    public string RepresentativeName { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public DateOnly BookingDate { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public int DurationHours { get; set; }
    public List<OrderItemDto> Items { get; set; } = new();
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = string.Empty;
    public string? PaymentProofUrl { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
}
