namespace KopiKala.Models;

public partial class Timeslot
{
    public int Id { get; set; }

    public string SessionName { get; set; } = null!;

    public TimeOnly StartTime { get; set; }

    public TimeOnly EndTime { get; set; }

    public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();
}
