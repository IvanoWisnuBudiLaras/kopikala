namespace KopiKala.DTOs.Booking;

public class TimeslotDto
{
    public int Id { get; set; }
    public string SessionName { get; set; } = string.Empty;
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
}
