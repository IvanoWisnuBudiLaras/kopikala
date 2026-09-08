using KopiKala.Data;
using KopiKala.Models;
using KopiKala.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;
using Moq;
using Xunit;
using Xunit.Abstractions;

namespace KopiKala.Tests.Integration;

public class TimeTravelerAdminTests : IDisposable
{
    private readonly ITestOutputHelper _output;
    private readonly ServiceProvider _serviceProvider;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly FakeTimeProvider _fakeTime;
    private readonly Mock<IWebHostEnvironment> _envMock;
    private readonly Mock<ILogger<SuperAdminService>> _loggerMock;
    private readonly SuperAdminService _adminService;

    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _tableId = Guid.NewGuid();
    private readonly Guid _menuItemId = Guid.NewGuid();

    public TimeTravelerAdminTests(ITestOutputHelper output)
    {
        _output = output;

        var dbName = Guid.NewGuid().ToString();
        var services = new ServiceCollection();
        services.AddDbContext<AppDbContext>(options =>
            options.UseInMemoryDatabase(databaseName: dbName));

        _serviceProvider = services.BuildServiceProvider();
        _scopeFactory = _serviceProvider.GetRequiredService<IServiceScopeFactory>();

        _envMock = new Mock<IWebHostEnvironment>();
        _loggerMock = new Mock<ILogger<SuperAdminService>>();

        _fakeTime = new FakeTimeProvider();
        _fakeTime.SetUtcNow(new DateTimeOffset(2026, 9, 8, 10, 0, 0, TimeSpan.Zero));

        _adminService = new SuperAdminService(_scopeFactory, _envMock.Object, _fakeTime, _loggerMock.Object);

        SeedData();
    }

    private AppDbContext GetContext() => _serviceProvider.CreateScope().ServiceProvider.GetRequiredService<AppDbContext>();

    private void SeedData()
    {
        using var context = GetContext();

        context.Users.Add(new User
        {
            Id = _userId,
            FullName = "Admin Tester",
            Email = "admintester@kopikala.com",
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
    public async Task TimeTraveler_AdminAnalytics_ReflectsDateAdvancement()
    {
        var day1 = new DateOnly(2026, 9, 8);
        var day2 = new DateOnly(2026, 9, 9);

        // Seed bookings for Day 1
        using (var context = GetContext())
        {
            context.Bookings.Add(new Booking
            {
                Id = Guid.NewGuid(),
                InvoiceCode = "INV/20260908/D1",
                UserId = _userId,
                TableId = _tableId,
                TimeslotId = 1,
                BookingDate = day1,
                DurationHours = 2,
                RepresentativeName = "Day 1 Guest",
                Status = "Selesai",
                PaymentMethod = "TransferBank",
                TotalAmount = 50000,
                BookingDetails = new List<BookingDetail>
                {
                    new() { Id = Guid.NewGuid(), MenuItemId = _menuItemId, Quantity = 2, UnitPrice = 25000, SubTotal = 50000, OrderType = "PreOrder" }
                }
            });

            // Seed DailyReport for Day 1
            context.DailyReports.Add(new DailyReport
            {
                Id = Guid.NewGuid(),
                ReportDate = day1,
                TotalRevenue = 50000,
                TotalBookings = 1,
                TotalNoShows = 0,
                TotalCancelled = 0,
                TopSellingItem = "Signature Kopi Susu"
            });

            await context.SaveChangesAsync();
        }

        // 1. Day 1 Check
        var analyticsDay1 = await _adminService.GetAnalyticsDashboardDataAsync();
        _output.WriteLine($"[TIME TRAVEL DAY 1] Revenue: {analyticsDay1.TodayRevenue:N0}, Bookings: {analyticsDay1.TotalBookingsToday}");
        Assert.Equal(50000, analyticsDay1.TodayRevenue);
        Assert.Equal(1, analyticsDay1.TotalBookingsToday);

        // 2. Advance time 24 hours to Day 2
        _fakeTime.Advance(TimeSpan.FromDays(1));
        _output.WriteLine($"[TIME TRAVEL] Advanced +1 Day -> Now: {_fakeTime.GetUtcNow():yyyy-MM-dd HH:mm:ss} UTC");

        // On Day 2 morning, today's live bookings are 0, but latest report gives reference revenue 50000
        var analyticsDay2 = await _adminService.GetAnalyticsDashboardDataAsync();
        _output.WriteLine($"[TIME TRAVEL DAY 2] Reference Revenue: {analyticsDay2.TodayRevenue:N0}, Today Bookings: {analyticsDay2.TotalBookingsToday}");
        Assert.Equal(50000, analyticsDay2.TodayRevenue);
        Assert.Equal(0, analyticsDay2.TotalBookingsToday);
    }
}
