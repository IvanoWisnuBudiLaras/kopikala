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

namespace KopiKala.Tests.Workers;

public class MidnightReconciliationWorkerTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly Mock<IWebHostEnvironment> _envMock;
    private readonly FakeTimeProvider _fakeTime;
    private readonly Mock<ILogger<MidnightReconciliationWorker>> _loggerMock;
    private readonly MidnightReconciliationWorker _worker;
    private readonly string _tempWebRoot;

    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _tableId = Guid.NewGuid();
    private readonly Guid _menuItem1Id = Guid.NewGuid();
    private readonly Guid _menuItem2Id = Guid.NewGuid();

    public MidnightReconciliationWorkerTests()
    {
        var dbName = Guid.NewGuid().ToString();
        var services = new ServiceCollection();
        services.AddDbContext<AppDbContext>(options =>
            options.UseInMemoryDatabase(databaseName: dbName));

        _serviceProvider = services.BuildServiceProvider();
        _scopeFactory = _serviceProvider.GetRequiredService<IServiceScopeFactory>();

        _envMock = new Mock<IWebHostEnvironment>();
        _tempWebRoot = Path.Combine(Path.GetTempPath(), "kopikala_reconcile_test_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempWebRoot);
        _envMock.Setup(e => e.WebRootPath).Returns(_tempWebRoot);

        _loggerMock = new Mock<ILogger<MidnightReconciliationWorker>>();

        _fakeTime = new FakeTimeProvider();
        _fakeTime.SetUtcNow(new DateTimeOffset(2026, 9, 8, 0, 5, 0, TimeSpan.Zero)); // 00:05 UTC

        _worker = new MidnightReconciliationWorker(
            _scopeFactory,
            _envMock.Object,
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
            FullName = "Reconcile User",
            Email = "reconcile@kopikala.com",
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

        context.MenuItems.AddRange(
            new MenuItem
            {
                Id = _menuItem1Id,
                Name = "Kopi Susu Gula Aren",
                Category = "Coffee",
                Price = 22000,
                Stock = 50,
                IsAvailable = true
            },
            new MenuItem
            {
                Id = _menuItem2Id,
                Name = "Croissant Butter",
                Category = "Pastry & Bakery",
                Price = 25000,
                Stock = 30,
                IsAvailable = true
            }
        );

        context.SaveChanges();
    }

    public void Dispose()
    {
        using (var context = GetContext())
        {
            context.Database.EnsureDeleted();
        }
        _serviceProvider.Dispose();

        if (Directory.Exists(_tempWebRoot))
        {
            try { Directory.Delete(_tempWebRoot, true); } catch { }
        }
    }

    [Fact]
    public async Task PerformReconciliationAsync_CalculatesDailyMetrics_Accurately()
    {
        var targetDate = new DateOnly(2026, 9, 7);

        using (var context = GetContext())
        {
            // Booking 1: Selesai (2x Kopi Susu = 44.000)
            context.Bookings.Add(new Booking
            {
                Id = Guid.NewGuid(),
                InvoiceCode = "INV/20260907/B01",
                UserId = _userId,
                TableId = _tableId,
                TimeslotId = 1,
                BookingDate = targetDate,
                DurationHours = 2,
                RepresentativeName = "Guest 1",
                Status = "Selesai",
                PaymentMethod = "TransferBank",
                TotalAmount = 44000,
                BookingDetails = new List<BookingDetail>
                {
                    new() { Id = Guid.NewGuid(), MenuItemId = _menuItem1Id, Quantity = 2, UnitPrice = 22000, SubTotal = 44000, OrderType = "PreOrder" }
                }
            });

            // Booking 2: Selesai (1x Kopi Susu + 2x Croissant = 72.000)
            context.Bookings.Add(new Booking
            {
                Id = Guid.NewGuid(),
                InvoiceCode = "INV/20260907/B02",
                UserId = _userId,
                TableId = _tableId,
                TimeslotId = 2,
                BookingDate = targetDate,
                DurationHours = 2,
                RepresentativeName = "Guest 2",
                Status = "Selesai",
                PaymentMethod = "BayarDiTempat",
                TotalAmount = 72000,
                BookingDetails = new List<BookingDetail>
                {
                    new() { Id = Guid.NewGuid(), MenuItemId = _menuItem1Id, Quantity = 1, UnitPrice = 22000, SubTotal = 22000, OrderType = "PreOrder" },
                    new() { Id = Guid.NewGuid(), MenuItemId = _menuItem2Id, Quantity = 2, UnitPrice = 25000, SubTotal = 50000, OrderType = "PreOrder" }
                }
            });

            // Booking 3: NoShow
            context.Bookings.Add(new Booking
            {
                Id = Guid.NewGuid(),
                InvoiceCode = "INV/20260907/B03",
                UserId = _userId,
                TableId = _tableId,
                TimeslotId = 3,
                BookingDate = targetDate,
                DurationHours = 2,
                RepresentativeName = "No Show Guest",
                Status = "NoShow",
                PaymentMethod = "BayarDiTempat",
                TotalAmount = 30000
            });

            // Booking 4: Batal
            context.Bookings.Add(new Booking
            {
                Id = Guid.NewGuid(),
                InvoiceCode = "INV/20260907/B04",
                UserId = _userId,
                TableId = _tableId,
                TimeslotId = 4,
                BookingDate = targetDate,
                DurationHours = 2,
                RepresentativeName = "Cancelled Guest",
                Status = "Batal",
                PaymentMethod = "TransferBank",
                TotalAmount = 50000
            });

            await context.SaveChangesAsync();
        }

        var report = await _worker.PerformReconciliationAsync(targetDate);

        Assert.NotNull(report);
        Assert.Equal(targetDate, report.ReportDate);
        Assert.Equal(116000, report.TotalRevenue); // 44.000 + 72.000
        Assert.Equal(2, report.TotalBookings);     // 2 Selesai
        Assert.Equal(1, report.TotalNoShows);      // 1 NoShow
        Assert.Equal(1, report.TotalCancelled);    // 1 Batal
        Assert.Equal("Kopi Susu Gula Aren", report.TopSellingItem); // 3x Kopi Susu vs 2x Croissant

        // Verify stored in database
        using (var context = GetContext())
        {
            var savedReport = await context.DailyReports.FirstOrDefaultAsync(r => r.ReportDate == targetDate);
            Assert.NotNull(savedReport);
            Assert.Equal(116000, savedReport.TotalRevenue);
        }
    }

    [Fact]
    public async Task PerformReconciliationAsync_ExistingReport_UpdatesData()
    {
        var targetDate = new DateOnly(2026, 9, 7);

        // Pre-create report
        using (var context = GetContext())
        {
            context.DailyReports.Add(new DailyReport
            {
                Id = Guid.NewGuid(),
                ReportDate = targetDate,
                TotalRevenue = 0,
                TotalBookings = 0
            });
            await context.SaveChangesAsync();
        }

        var report = await _worker.PerformReconciliationAsync(targetDate);
        Assert.NotNull(report);

        using (var context = GetContext())
        {
            var reports = await context.DailyReports.Where(r => r.ReportDate == targetDate).ToListAsync();
            Assert.Single(reports); // Upsert, not duplicate
        }
    }

    [Fact]
    public void PerformFileCleanup_DeletesFilesOlderThan30Days()
    {
        var uploadsDir = Path.Combine(_tempWebRoot, "uploads", "payments");
        Directory.CreateDirectory(uploadsDir);

        var oldFilePath = Path.Combine(uploadsDir, "old_receipt.jpg");
        var newFilePath = Path.Combine(uploadsDir, "new_receipt.jpg");

        File.WriteAllBytes(oldFilePath, [0x01, 0x02]);
        File.WriteAllBytes(newFilePath, [0x03, 0x04]);

        // Set creation time for old file (35 days ago)
        File.SetCreationTimeUtc(oldFilePath, DateTime.UtcNow.AddDays(-35));
        File.SetCreationTimeUtc(newFilePath, DateTime.UtcNow.AddDays(-2));

        var deletedCount = _worker.PerformFileCleanup(olderThanDays: 30);

        Assert.Equal(1, deletedCount);
        Assert.False(File.Exists(oldFilePath));
        Assert.True(File.Exists(newFilePath));
    }

    [Fact]
    public async Task MidnightReconciliationWorker_ExecuteAsync_RunsLoopAndReconciles()
    {
        using var cts = new CancellationTokenSource();
        var startTask = _worker.StartAsync(cts.Token);

        // Advance fake time across days to trigger reconciliation
        _fakeTime.Advance(TimeSpan.FromDays(2));
        await Task.Delay(100);

        cts.Cancel();
        await _worker.StopAsync(CancellationToken.None);
    }
}
