namespace KopiKala.DTOs.Staff;

public class CashierBookingDto
{
    public Guid BookingId { get; set; }
    public string InvoiceCode { get; set; } = string.Empty;
    public string RepresentativeName { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public Guid TableId { get; set; }
    public string TableNumber { get; set; } = string.Empty;
    public string TableArea { get; set; } = string.Empty;
    public int TableCapacity { get; set; }
    public int TimeslotId { get; set; }
    public string TimeslotName { get; set; } = string.Empty;
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public int DurationHours { get; set; }
    public DateOnly BookingDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = string.Empty;
    public string? PaymentProofUrl { get; set; }
    public decimal TotalAmount { get; set; }
    public DateTime? SeatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<CashierOrderItemDto> Items { get; set; } = new();
}
