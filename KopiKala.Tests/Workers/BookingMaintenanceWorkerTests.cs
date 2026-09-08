using KopiKala.Data;
using KopiKala.Models;
using KopiKala.Workers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;
using Moq;
using Xunit;

namespace KopiKala.Tests.Workers;

public class BookingMaintenanceWorkerTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly FakeTimeProvider _fakeTime;
    private readonly Mock<ILogger<BookingMaintenanceWorker>> _loggerMock;
    private readonly BookingMaintenanceWorker _worker;

    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _tableId = Guid.NewGuid();

    public BookingMaintenanceWorkerTests()
    {
        var dbName = Guid.NewGuid().ToString();
        var services = new ServiceCollection();
        services.AddDbContext<AppDbContext>(options =>
            options.UseInMemoryDatabase(databaseName: dbName));

        _serviceProvider = services.BuildServiceProvider();
        _scopeFactory = _serviceProvider.GetRequiredService<IServiceScopeFactory>();

        _loggerMock = new Mock<ILogger<BookingMaintenanceWorker>>();

        _fakeTime = new FakeTimeProvider();
        _fakeTime.SetUtcNow(new DateTimeOffset(2026, 9, 8, 10, 0, 0, TimeSpan.Zero));

        _worker = new BookingMaintenanceWorker(
            _scopeFactory,
            _fakeTime,
            _loggerMock.Object,
            TimeSpan.FromMilliseconds(50));

        SeedData();
    }

    private AppDbContext GetContext() => _serviceProvider.CreateScope().ServiceProvider.GetRequiredService<AppDbContext>();

    private void SeedData()
    {
        using var context = GetContext();
        context.Users.Add(new User
        {
            Id = _userId,
            FullName = "Worker Test User",
            Email = "worker@kopikala.com",
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

        context.Timeslots.Add(new Timeslot
        {
            Id = 2,
            SessionName = "Sesi Siang (11:00 - 13:00)",
            StartTime = new TimeOnly(11, 0),
            EndTime = new TimeOnly(13, 0)
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
    public async Task PerformMaintenanceCycleAsync_AutoCancels_ExpiredTransferBookings()
    {
        var nowUtc = _fakeTime.GetUtcNow().UtcDateTime;

        using (var context = GetContext())
        {
            // Booking transfer yang sudah expired (ExpiresAt 5 menit lalu)
            context.Bookings.Add(new Booking
            {
                Id = Guid.NewGuid(),
                InvoiceCode = "INV/20260908/EXP01",
                UserId = _userId,
                TableId = _tableId,
                TimeslotId = 1,
                BookingDate = new DateOnly(2026, 9, 8),
                DurationHours = 2,
                RepresentativeName = "Expired Transfer User",
                Status = "MenungguBayar",
                PaymentMethod = "TransferBank",
                TotalAmount = 50000,
                ExpiresAt = nowUtc.AddMinutes(-5),
                CreatedAt = nowUtc.AddMinutes(-20)
            });

            // Booking transfer yang belum expired (ExpiresAt 10 menit lagi)
            context.Bookings.Add(new Booking
            {
                Id = Guid.NewGuid(),
                InvoiceCode = "INV/20260908/VAL01",
                UserId = _userId,
                TableId = _tableId,
                TimeslotId = 2,
                BookingDate = new DateOnly(2026, 9, 8),
                DurationHours = 2,
                RepresentativeName = "Valid Transfer User",
                Status = "MenungguBayar",
                PaymentMethod = "TransferBank",
                TotalAmount = 50000,
                ExpiresAt = nowUtc.AddMinutes(10),
                CreatedAt = nowUtc.AddMinutes(-5)
            });

            await context.SaveChangesAsync();
        }

        await _worker.PerformMaintenanceCycleAsync();

        using (var context = GetContext())
        {
            var expired = await context.Bookings.FirstAsync(b => b.InvoiceCode == "INV/20260908/EXP01");
            var valid = await context.Bookings.FirstAsync(b => b.InvoiceCode == "INV/20260908/VAL01");

            Assert.Equal("Batal", expired.Status);
            Assert.Equal("MenungguBayar", valid.Status);
        }
    }

    [Fact]
    public async Task PerformMaintenanceCycleAsync_TriggersNoShowAlert_WhenLateOver20Min()
    {
        // Now is 10:00:00. Timeslot 1 starts at 09:00:00. Tolerance 20m is 09:20:00.
        // Current time (10:00:00) >= 09:20:00 -> should trigger PeringatanNoShow.
        var today = new DateOnly(2026, 9, 8);
        var nowUtc = _fakeTime.GetUtcNow().UtcDateTime;

        using (var context = GetContext())
        {
            context.Bookings.Add(new Booking
            {
                Id = Guid.NewGuid(),
                InvoiceCode = "INV/20260908/NOSHOW",
                UserId = _userId,
                TableId = _tableId,
                TimeslotId = 1,
                BookingDate = today,
                DurationHours = 2,
                RepresentativeName = "Late Guest",
                Status = "Dikonfirmasi",
                PaymentMethod = "BayarDiTempat",
                TotalAmount = 40000,
                ExpiresAt = nowUtc.AddHours(2),
                CreatedAt = nowUtc.AddHours(-1)
            });

            // Future booking on same day (Timeslot 2 starts at 11:00, now is 10:00 -> not late)
            context.Bookings.Add(new Booking
            {
                Id = Guid.NewGuid(),
                InvoiceCode = "INV/20260908/ONTIME",
                UserId = _userId,
                TableId = _tableId,
                TimeslotId = 2,
                BookingDate = today,
                DurationHours = 2,
                RepresentativeName = "Future Guest",
                Status = "Dikonfirmasi",
                PaymentMethod = "BayarDiTempat",
                TotalAmount = 40000,
                ExpiresAt = nowUtc.AddHours(3),
                CreatedAt = nowUtc
            });

            await context.SaveChangesAsync();
        }

        await _worker.PerformMaintenanceCycleAsync();

        using (var context = GetContext())
        {
            var lateBooking = await context.Bookings.FirstAsync(b => b.InvoiceCode == "INV/20260908/NOSHOW");
            var onTimeBooking = await context.Bookings.FirstAsync(b => b.InvoiceCode == "INV/20260908/ONTIME");

            Assert.Equal("PeringatanNoShow", lateBooking.Status);
            Assert.Equal("Dikonfirmasi", onTimeBooking.Status);
        }
    }

    [Fact]
    public async Task PerformMaintenanceCycleAsync_TriggersWaktuHabis_WhenSeatedDurationExpires()
    {
        var nowUtc = _fakeTime.GetUtcNow().UtcDateTime;

        using (var context = GetContext())
        {
            context.Bookings.Add(new Booking
            {
                Id = Guid.NewGuid(),
                InvoiceCode = "INV/20260908/SEATED_EXPIRED",
                UserId = _userId,
                TableId = _tableId,
                TimeslotId = 1,
                BookingDate = new DateOnly(2026, 9, 8),
                DurationHours = 2,
                RepresentativeName = "Seated Expired Guest",
                Status = "SedangDigunakan",
                PaymentMethod = "BayarDiTempat",
                TotalAmount = 50000,
                SeatedAt = nowUtc.AddHours(-2),
                ExpiresAt = nowUtc.AddMinutes(-1), // Duration expired 1 minute ago
                CreatedAt = nowUtc.AddHours(-2)
            });

            context.Bookings.Add(new Booking
            {
                Id = Guid.NewGuid(),
                InvoiceCode = "INV/20260908/SEATED_ACTIVE",
                UserId = _userId,
                TableId = _tableId,
                TimeslotId = 1,
                BookingDate = new DateOnly(2026, 9, 8),
                DurationHours = 2,
                RepresentativeName = "Seated Active Guest",
                Status = "SedangDigunakan",
                PaymentMethod = "BayarDiTempat",
                TotalAmount = 50000,
                SeatedAt = nowUtc.AddHours(-1),
                ExpiresAt = nowUtc.AddHours(1), // Still 1 hour remaining
                CreatedAt = nowUtc.AddHours(-1)
            });

            await context.SaveChangesAsync();
        }

        await _worker.PerformMaintenanceCycleAsync();

        using (var context = GetContext())
        {
            var expired = await context.Bookings.FirstAsync(b => b.InvoiceCode == "INV/20260908/SEATED_EXPIRED");
            var active = await context.Bookings.FirstAsync(b => b.InvoiceCode == "INV/20260908/SEATED_ACTIVE");

            Assert.Equal("WaktuHabis", expired.Status);
            Assert.Equal("SedangDigunakan", active.Status);
        }
    }

    [Fact]
    public async Task BookingMaintenanceWorker_ExecuteAsync_RunsPeriodicLoopAndStops()
    {
        using var cts = new CancellationTokenSource();
        var startTask = _worker.StartAsync(cts.Token);

        // Advance fake time to trigger timer ticks
        _fakeTime.Advance(TimeSpan.FromSeconds(30));
        await Task.Delay(100);

        cts.Cancel();
        await _worker.StopAsync(CancellationToken.None);
    }
}
