using KopiKala.Data;
using KopiKala.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace KopiKala.Workers;

public class MidnightReconciliationWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IWebHostEnvironment _env;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<MidnightReconciliationWorker> _logger;
    private readonly TimeSpan _checkInterval;
    private DateOnly _lastReconciledDate;

    public MidnightReconciliationWorker(
        IServiceScopeFactory scopeFactory,
        IWebHostEnvironment env,
        TimeProvider timeProvider,
        ILogger<MidnightReconciliationWorker> logger,
        TimeSpan? checkInterval = null)
    {
        _scopeFactory = scopeFactory;
        _env = env;
        _timeProvider = timeProvider;
        _logger = logger;
        _checkInterval = checkInterval ?? TimeSpan.FromMinutes(1);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Midnight Reconciliation Worker started.");

        using var timer = new PeriodicTimer(_checkInterval, _timeProvider);

        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                try
                {
                    var now = _timeProvider.GetUtcNow().UtcDateTime;
                    var today = DateOnly.FromDateTime(now);
                    var yesterday = today.AddDays(-1);

                    // Run reconciliation once per day when date changes
                    if (_lastReconciledDate < yesterday)
                    {
                        await PerformReconciliationAsync(yesterday, stoppingToken);
                        _lastReconciledDate = yesterday;

                        // Weekly cleanup on Sunday nights
                        if (now.DayOfWeek == DayOfWeek.Sunday)
                        {
                            PerformFileCleanup();
                        }
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogError(ex, "Error occurred in MidnightReconciliationWorker execution cycle.");
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Expected during graceful shutdown
        }

        _logger.LogInformation("Midnight Reconciliation Worker stopped.");
    }

    public async Task<DailyReport> PerformReconciliationAsync(DateOnly reportDate, CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var bookings = await db.Bookings
            .Include(b => b.BookingDetails)
                .ThenInclude(d => d.MenuItem)
            .Where(b => b.BookingDate == reportDate)
            .ToListAsync(cancellationToken);

        var completedBookings = bookings.Where(b => b.Status == "Selesai").ToList();
        var totalRevenue = completedBookings.Sum(b => b.TotalAmount);
        var totalBookings = completedBookings.Count;
        var totalNoShows = bookings.Count(b => b.Status == "NoShow" || b.Status == "PeringatanNoShow");
        var totalCancelled = bookings.Count(b => b.Status == "Batal" || b.Status == "Kedaluwarsa");

        // Top selling item by quantity
        var topSellingItem = completedBookings
            .SelectMany(b => b.BookingDetails)
            .GroupBy(d => d.MenuItem != null ? d.MenuItem.Name : "Unknown")
            .OrderByDescending(g => g.Sum(x => x.Quantity))
            .Select(g => g.Key)
            .FirstOrDefault() ?? "Tidak ada transaksi";

        var existingReport = await db.DailyReports
            .FirstOrDefaultAsync(r => r.ReportDate == reportDate, cancellationToken);

        if (existingReport == null)
        {
            existingReport = new DailyReport
            {
                Id = Guid.NewGuid(),
                ReportDate = reportDate,
                TotalRevenue = totalRevenue,
                TotalBookings = totalBookings,
                TotalNoShows = totalNoShows,
                TotalCancelled = totalCancelled,
                TopSellingItem = topSellingItem,
                GeneratedAt = _timeProvider.GetUtcNow().UtcDateTime
            };
            db.DailyReports.Add(existingReport);
        }
        else
        {
            existingReport.TotalRevenue = totalRevenue;
            existingReport.TotalBookings = totalBookings;
            existingReport.TotalNoShows = totalNoShows;
            existingReport.TotalCancelled = totalCancelled;
            existingReport.TopSellingItem = topSellingItem;
            existingReport.GeneratedAt = _timeProvider.GetUtcNow().UtcDateTime;
        }

        await db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Midnight Reconciliation for {Date}: Revenue=Rp{Revenue}, Bookings={Bookings}, NoShows={NoShows}, Cancelled={Cancelled}, TopItem={TopItem}",
            reportDate, totalRevenue, totalBookings, totalNoShows, totalCancelled, topSellingItem);

        return existingReport;
    }

    public int PerformFileCleanup(int olderThanDays = 30)
    {
        try
        {
            var uploadsDir = Path.Combine(_env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"), "uploads", "payments");
            if (!Directory.Exists(uploadsDir))
            {
                return 0;
            }

            var cutoff = _timeProvider.GetUtcNow().UtcDateTime.AddDays(-olderThanDays);
            var files = Directory.GetFiles(uploadsDir);
            int deletedCount = 0;

            foreach (var file in files)
            {
                var fileInfo = new FileInfo(file);
                if (fileInfo.CreationTimeUtc < cutoff)
                {
                    try
                    {
                        File.Delete(file);
                        deletedCount++;
                        _logger.LogInformation("Deleted old payment proof file: {FileName}", fileInfo.Name);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to delete old file: {FileName}", fileInfo.Name);
                    }
                }
            }

            _logger.LogInformation("Weekly file cleanup completed: {Count} files deleted.", deletedCount);
            return deletedCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error performing weekly file cleanup.");
            return 0;
        }
    }
}
