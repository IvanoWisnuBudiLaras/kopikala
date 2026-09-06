namespace KopiKala.Models;

public partial class BookingDetail
{
    public Guid Id { get; set; }

    public Guid BookingId { get; set; }

    public Guid MenuItemId { get; set; }

    public int Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal SubTotal { get; set; }

    public string OrderType { get; set; } = null!;

    public virtual Booking Booking { get; set; } = null!;

    public virtual MenuItem MenuItem { get; set; } = null!;
}
