namespace KopiKala.DTOs.Staff;

public class CashierTableStatusDto
{
    public Guid TableId { get; set; }
    public string TableNumber { get; set; } = string.Empty;
    public int Capacity { get; set; }
    public string Area { get; set; } = string.Empty;
    public string DisplayStatus { get; set; } = "Tersedia"; // "Tersedia", "SedangDigunakan", "Dikonfirmasi", "MenungguVerifikasi", "MenungguBayar"
    public Guid? CurrentBookingId { get; set; }
    public string? CurrentInvoiceCode { get; set; }
    public string? CurrentRepresentativeName { get; set; }
    public TimeOnly? StartTime { get; set; }
    public TimeOnly? EndTime { get; set; }
    public DateTime? SeatedAt { get; set; }
    public decimal TotalAmount { get; set; }
}
