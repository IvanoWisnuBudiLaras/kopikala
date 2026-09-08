namespace KopiKala.Models;

public partial class DailyReport
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public DateOnly ReportDate { get; set; }

    public decimal TotalRevenue { get; set; }

    public int TotalBookings { get; set; }

    public int TotalNoShows { get; set; }

    public int TotalCancelled { get; set; }

    public string TopSellingItem { get; set; } = string.Empty;

    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}
