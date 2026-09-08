using KopiKala.Data;
using KopiKala.Models;
using KopiKala.Workers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;
using Moq;
using Xunit;
using Xunit.Abstractions;

namespace KopiKala.Tests.Integration;

public class TimeTravelerWorkerTests : IDisposable
{
    private readonly ITestOutputHelper _output;
    private readonly ServiceProvider _serviceProvider;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly FakeTimeProvider _fakeTime;
    private readonly Mock<IWebHostEnvironment> _envMock;
    private readonly Mock<ILogger<BookingMaintenanceWorker>> _maintenanceLoggerMock;
    private readonly Mock<ILogger<MidnightReconciliationWorker>> _reconcileLoggerMock;
    private readonly BookingMaintenanceWorker _maintenanceWorker;
    private readonly MidnightReconciliationWorker _reconcileWorker;

    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _tableId = Guid.NewGuid();
    private readonly Guid _menuItemId = Guid.NewGuid();

    public TimeTravelerWorkerTests(ITestOutputHelper output)
    {
        _output = output;

        var dbName = Guid.NewGuid().ToString();
        var services = new ServiceCollection();
        services.AddDbContext<AppDbContext>(options =>
            options.UseInMemoryDatabase(databaseName: dbName));

        _serviceProvider = services.BuildServiceProvider();
        _scopeFactory = _serviceProvider.GetRequiredService<IServiceScopeFactory>();

        _envMock = new Mock<IWebHostEnvironment>();
        _maintenanceLoggerMock = new Mock<ILogger<BookingMaintenanceWorker>>();
        _reconcileLoggerMock = new Mock<ILogger<MidnightReconciliationWorker>>();

        _fakeTime = new FakeTimeProvider();
        _fakeTime.SetUtcNow(new DateTimeOffset(2026, 9, 8, 9, 0, 0, TimeSpan.Zero)); // 09:00:00 UTC

        _maintenanceWorker = new BookingMaintenanceWorker(
            _scopeFactory,
            _fakeTime,
            _maintenanceLoggerMock.Object,
            TimeSpan.FromSeconds(30));

        _reconcileWorker = new MidnightReconciliationWorker(
            _scopeFactory,
            _envMock.Object,
            _fakeTime,
            _reconcileLoggerMock.Object,
            TimeSpan.FromMinutes(1));

        SeedData();
    }

    private AppDbContext GetContext() => _serviceProvider.CreateScope().ServiceProvider.GetRequiredService<AppDbContext>();

    private void SeedData()
    {
        using var context = GetContext();
        context.Users.Add(new User
        {
            Id = _userId,
            FullName = "Time Traveler Worker User",
            Email = "worker_tt@kopikala.com",
            PhoneNumber = "081234567890",
            PasswordHash = "hash",
            CreatedAt = DateTime.UtcNow
        });

        context.DiningTables.Add(new DiningTable
        {
            Id = _tableId,
            TableNumber = "IN-01",
            Capacity = 4,
            Area = "Indoor AC",
            IsActive = true
        });

        context.Timeslots.Add(new Timeslot
        {
            Id = 1,
            SessionName = "Sesi Pagi (09:00 - 11:00)",
            StartTime = new TimeOnly(9, 0),
            EndTime = new TimeOnly(11, 0)
        });

        context.MenuItems.Add(new MenuItem
        {
            Id = _menuItemId,
            Name = "Signature Kopi Susu",
            Category = "Coffee",
            Price = 22000,
            Stock = 50,
            IsAvailable = true
        });

        context.SaveChanges();
    }

    public void Dispose()
    {
        using (var context = GetContext())
        {
            context.Database.EnsureDeleted();
        }
        _serviceProvider.Dispose();
    }

    [Fact]
    public async Task TimeTraveler_FullTriTierCycle_SimulatesAllTimeWindows()
    {
        var today = new DateOnly(2026, 9, 8);
        var t0 = _fakeTime.GetUtcNow().UtcDateTime;

        _output.WriteLine($"=== STARTING TRI-TIER TIME TRAVEL SIMULATION ===");
        _output.WriteLine($"[T = 00:00] Simulation Time: {t0:yyyy-MM-dd HH:mm:ss} UTC");

        Guid transferBookingId;
        Guid payOnSiteBookingId;
        Guid seatedBookingId;

        // Setup 3 bookings with different workflows
        using (var context = GetContext())
        {
            // 1. Transfer booking waiting payment (15m limit -> expires at 09:15)
            var b1 = new Booking
            {
                Id = Guid.NewGuid(),
                InvoiceCode = "INV/20260908/TRF01",
                UserId = _userId,
                TableId = _tableId,
                TimeslotId = 1,
                BookingDate = today,
                DurationHours = 2,
                RepresentativeName = "Transfer Guest",
                Status = "MenungguBayar",
                PaymentMethod = "TransferBank",
                TotalAmount = 44000,
                ExpiresAt = t0.AddMinutes(15),
                CreatedAt = t0
            };
            context.Bookings.Add(b1);
            transferBookingId = b1.Id;

            // 2. PayOnSite booking (Timeslot 09:00, tolerance 20m -> alert at 09:20)
            var b2 = new Booking
            {
                Id = Guid.NewGuid(),
                InvoiceCode = "INV/20260908/POS01",
                UserId = _userId,
                TableId = _tableId,
                TimeslotId = 1,
                BookingDate = today,
                DurationHours = 2,
                RepresentativeName = "PayOnSite Guest",
                Status = "Dikonfirmasi",
                PaymentMethod = "BayarDiTempat",
                TotalAmount = 50000,
                ExpiresAt = t0.AddHours(2),
                CreatedAt = t0
            };
            context.Bookings.Add(b2);
            payOnSiteBookingId = b2.Id;

            // 3. Seated guest (Duration 2 hours -> expires at 11:00)
            var b3 = new Booking
            {
                Id = Guid.NewGuid(),
                InvoiceCode = "INV/20260908/SEAT01",
                UserId = _userId,
                TableId = _tableId,
                TimeslotId = 1,
                BookingDate = today,
                DurationHours = 2,
                RepresentativeName = "Seated Guest",
                Status = "SedangDigunakan",
                PaymentMethod = "BayarDiTempat",
                TotalAmount = 66000,
                SeatedAt = t0,
                ExpiresAt = t0.AddHours(2),
                CreatedAt = t0,
                BookingDetails = new List<BookingDetail>
                {
                    new() { Id = Guid.NewGuid(), MenuItemId = _menuItemId, Quantity = 3, UnitPrice = 22000, SubTotal = 66000, OrderType = "PreOrder" }
                }
            };
            context.Bookings.Add(b3);
            seatedBookingId = b3.Id;

            await context.SaveChangesAsync();
        }

        // --- STEP 1: Advance time +16 min (T = 09:16) -> Transfer Timeout ---
        _fakeTime.Advance(TimeSpan.FromMinutes(16));
        _output.WriteLine($"\n[STEP 1] Advanced +16 min -> Now: {_fakeTime.GetUtcNow():yyyy-MM-dd HH:mm:ss} UTC");

        await _maintenanceWorker.PerformMaintenanceCycleAsync();

        using (var context = GetContext())
        {
            var b1 = await context.Bookings.FindAsync(transferBookingId);
            Assert.Equal("Batal", b1!.Status);
            _output.WriteLine($"[AUTO-CANCEL 15M] Transfer Booking {b1.InvoiceCode} status is now: {b1.Status} (PASSED)");
        }

        // --- STEP 2: Advance time +5 min (Total +21 min, T = 09:21) -> No-Show Alert ---
        _fakeTime.Advance(TimeSpan.FromMinutes(5));
        _output.WriteLine($"\n[STEP 2] Advanced +5 min (Total +21m) -> Now: {_fakeTime.GetUtcNow():yyyy-MM-dd HH:mm:ss} UTC");

        await _maintenanceWorker.PerformMaintenanceCycleAsync();

        using (var context = GetContext())
        {
            var b2 = await context.Bookings.FindAsync(payOnSiteBookingId);
            Assert.Equal("PeringatanNoShow", b2!.Status);
            _output.WriteLine($"[NO-SHOW ALERT 20M] PayOnSite Booking {b2.InvoiceCode} status is now: {b2.Status} (PASSED)");
        }

        // --- STEP 3: Advance time to T = 11:01 (Total +2h 1m) -> WaktuHabis Alert ---
        _fakeTime.Advance(TimeSpan.FromHours(1).Add(TimeSpan.FromMinutes(40))); // From 09:21 to 11:01
        _output.WriteLine($"\n[STEP 3] Advanced to 11:01 -> Now: {_fakeTime.GetUtcNow():yyyy-MM-dd HH:mm:ss} UTC");

        await _maintenanceWorker.PerformMaintenanceCycleAsync();

        using (var context = GetContext())
        {
            var b3 = await context.Bookings.FindAsync(seatedBookingId);
            Assert.Equal("WaktuHabis", b3!.Status);
            _output.WriteLine($"[HOSPITALITY DURATION 2H] Seated Booking {b3.InvoiceCode} status is now: {b3.Status} (PASSED)");

            // Complete the booking for midnight reconciliation
            b3.Status = "Selesai";
            await context.SaveChangesAsync();
        }

        // --- STEP 4: Advance time to Midnight T = 00:05 (Next Day 2026-09-09) -> Reconciliation ---
        _fakeTime.Advance(TimeSpan.FromHours(13).Add(TimeSpan.FromMinutes(4))); // 11:01 to 00:05 next day
        _output.WriteLine($"\n[STEP 4] Advanced to Midnight Next Day -> Now: {_fakeTime.GetUtcNow():yyyy-MM-dd HH:mm:ss} UTC");

        var report = await _reconcileWorker.PerformReconciliationAsync(today);

        _output.WriteLine($"[MIDNIGHT CRON RECONCILIATION] Generated Report for {report.ReportDate}:");
        _output.WriteLine($"  - Total Revenue: Rp {report.TotalRevenue:N0}");
        _output.WriteLine($"  - Total Completed Bookings: {report.TotalBookings}");
        _output.WriteLine($"  - Total No-Shows: {report.TotalNoShows}");
        _output.WriteLine($"  - Total Cancelled: {report.TotalCancelled}");
        _output.WriteLine($"  - Top Selling Item: {report.TopSellingItem}");

        Assert.Equal(today, report.ReportDate);
        Assert.Equal(66000, report.TotalRevenue);
        Assert.Equal(1, report.TotalBookings);
        Assert.Equal(1, report.TotalNoShows);
        Assert.Equal(1, report.TotalCancelled);
        Assert.Equal("Signature Kopi Susu", report.TopSellingItem);

        _output.WriteLine($"\n=== ALL TRI-TIER TIME TRAVEL TESTS PASSED SUCCESSFULLY ===");
    }
}
