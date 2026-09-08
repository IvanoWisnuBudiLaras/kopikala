namespace KopiKala.DTOs.Admin;

public class AnalyticsDashboardDto
{
    public decimal TodayRevenue { get; set; }

    public int TotalBookingsToday { get; set; }

    public int TotalCompletedToday { get; set; }

    public int TotalNoShowsToday { get; set; }

    public int TotalCancelledToday { get; set; }

    public int TotalTables { get; set; }

    public int OccupiedTablesNow { get; set; }

    public int AvailableTablesNow { get; set; }

    public string ActiveOccupancyRate { get; set; } = "0%";

    public List<TopSellingItemDto> TopSellingItems { get; set; } = new();

    public double[] OccupancyDonutData { get; set; } = [0, 0];

    public string[] OccupancyDonutLabels { get; set; } = ["Tersedia", "Terisi"];
}

public class TopSellingItemDto
{
    public string Name { get; set; } = string.Empty;

    public int TotalQuantity { get; set; }

    public decimal TotalRevenue { get; set; }
}
