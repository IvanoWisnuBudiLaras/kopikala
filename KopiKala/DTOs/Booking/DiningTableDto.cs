namespace KopiKala.DTOs.Booking;

public class DiningTableDto
{
    public Guid Id { get; set; }
    public string TableNumber { get; set; } = string.Empty;
    public int Capacity { get; set; }
    public string Area { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public bool IsAvailable { get; set; } = true;
}
