namespace KopiKala.Models;

public partial class DiningTable
{
    public Guid Id { get; set; }

    public string TableNumber { get; set; } = null!;

    public int Capacity { get; set; }

    public string Area { get; set; } = null!;

    public bool IsActive { get; set; }

    public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();
}
