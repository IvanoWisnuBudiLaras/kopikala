using KopiKala.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace KopiKala.Workers;

public class BookingMaintenanceWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<BookingMaintenanceWorker> _logger;
    private readonly TimeSpan _checkInterval;

    public BookingMaintenanceWorker(
        IServiceScopeFactory scopeFactory,
        TimeProvider timeProvider,
        ILogger<BookingMaintenanceWorker> logger,
        TimeSpan? checkInterval = null)
    {
        _scopeFactory = scopeFactory;
        _timeProvider = timeProvider;
        _logger = logger;
        _checkInterval = checkInterval ?? TimeSpan.FromSeconds(30);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Booking Maintenance Worker started with check interval {Interval}.", _checkInterval);

        using var timer = new PeriodicTimer(_checkInterval, _timeProvider);

        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                try
                {
                    await PerformMaintenanceCycleAsync(stoppingToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogError(ex, "Error occurred in BookingMaintenanceWorker execution cycle.");
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Expected during graceful shutdown
        }

        _logger.LogInformation("Booking Maintenance Worker stopped.");
    }

    public async Task PerformMaintenanceCycleAsync(CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;
        var today = DateOnly.FromDateTime(nowUtc);
        var currentTime = TimeOnly.FromTimeSpan(nowUtc.TimeOfDay);

        bool hasChanges = false;

        // 1. AUTO-CANCEL: Batas Waktu Transfer 15 Menit Habis
        var expiredTransferBookings = await db.Bookings
            .Where(b => b.Status == "MenungguBayar" && b.ExpiresAt <= nowUtc)
            .ToListAsync(cancellationToken);

        foreach (var booking in expiredTransferBookings)
        {
            booking.Status = "Batal";
            hasChanges = true;
            _logger.LogInformation("Worker: Booking #{Code} dibatalkan otomatis (timeout transfer bank 15 menit).", booking.InvoiceCode);
        }

        // 2. HOSPITALITY ALERT: Toleransi No-Show 20 Menit Habis (Bayar Di Tempat)
        var noShowBookings = await db.Bookings
            .Include(b => b.Timeslot)
            .Where(b => b.Status == "Dikonfirmasi" &&
                        b.PaymentMethod == "BayarDiTempat" &&
                        b.BookingDate == today)
            .ToListAsync(cancellationToken);

        foreach (var booking in noShowBookings)
        {
            var toleranceTime = booking.Timeslot.StartTime.AddMinutes(20);
            if (currentTime >= toleranceTime)
            {
                booking.Status = "PeringatanNoShow";
                hasChanges = true;
                _logger.LogWarning("Worker: Booking #{Code} memerlukan konfirmasi kasir (peringatan NoShow > 20 menit).", booking.InvoiceCode);
            }
        }

        // 3. HOSPITALITY ALERT: Durasi Duduk Selesai (Peringatan Waktu Habis ke Staf)
        var completedDurationBookings = await db.Bookings
            .Where(b => (b.Status == "SedangDuduk" || b.Status == "SedangDigunakan") && b.ExpiresAt <= nowUtc)
            .ToListAsync(cancellationToken);

        foreach (var booking in completedDurationBookings)
        {
            booking.Status = "WaktuHabis";
            hasChanges = true;
            _logger.LogInformation("Worker: Booking #{Code} durasi duduk habis (WaktuHabis). Kasir/pelayan dapat cek jadwal & menawarkan perpanjangan jika slot kosong.", booking.InvoiceCode);
        }

        // 4. SCHEDULER: Log pengingat H-1 Jam
        var upcomingBookings = await db.Bookings
            .Include(b => b.Timeslot)
            .Where(b => b.Status == "Dikonfirmasi" &&
                        b.BookingDate == today)
            .ToListAsync(cancellationToken);

        foreach (var booking in upcomingBookings)
        {
            var reminderTime = booking.Timeslot.StartTime.AddHours(-1);
            if (currentTime >= reminderTime && currentTime < booking.Timeslot.StartTime)
            {
                _logger.LogInformation("Worker Scheduler: Pengingat H-1 jam untuk booking #{Code} ({Session}).", booking.InvoiceCode, booking.Timeslot.SessionName);
            }
        }

        if (hasChanges)
        {
            await db.SaveChangesAsync(cancellationToken);
        }
    }
}
