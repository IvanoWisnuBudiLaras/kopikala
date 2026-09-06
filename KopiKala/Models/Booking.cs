namespace KopiKala.Models;

public partial class Booking
{
    public Guid Id { get; set; }

    public string InvoiceCode { get; set; } = null!;

    public Guid UserId { get; set; }

    public Guid TableId { get; set; }

    public int TimeslotId { get; set; }

    public DateOnly BookingDate { get; set; }

    public int DurationHours { get; set; }

    public string RepresentativeName { get; set; } = null!;

    public string Status { get; set; } = null!;

    public string PaymentMethod { get; set; } = null!;

    public string? PaymentProofUrl { get; set; }

    public decimal TotalAmount { get; set; }

    public DateTime? SeatedAt { get; set; }

    public DateTime ExpiresAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<BookingDetail> BookingDetails { get; set; } = new List<BookingDetail>();

    public virtual DiningTable Table { get; set; } = null!;

    public virtual Timeslot Timeslot { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
