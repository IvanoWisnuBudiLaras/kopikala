using KopiKala.Data;
using KopiKala.DTOs.Booking;
using KopiKala.Models;
using KopiKala.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;
using Moq;
using Xunit;
using Xunit.Abstractions;

namespace KopiKala.Tests.Integration;

public class TimeTravelerBookingTests : IDisposable
{
    private readonly ITestOutputHelper _output;
    private readonly AppDbContext _context;
    private readonly Mock<IWebHostEnvironment> _envMock;
    private readonly FakeTimeProvider _fakeTime;
    private readonly Mock<ILogger<BookingService>> _loggerMock;
    private readonly BookingService _bookingService;

    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _tableId = Guid.NewGuid();

    public TimeTravelerBookingTests(ITestOutputHelper output)
    {
        _output = output;

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new AppDbContext(options);
        _envMock = new Mock<IWebHostEnvironment>();
        _loggerMock = new Mock<ILogger<BookingService>>();

        _fakeTime = new FakeTimeProvider();
        _fakeTime.SetUtcNow(new DateTimeOffset(2026, 9, 7, 10, 0, 0, TimeSpan.Zero));

        _bookingService = new BookingService(_context, _envMock.Object, _fakeTime, _loggerMock.Object);

        SeedData();
    }

    private void SeedData()
    {
        _context.Users.Add(new User
        {
            Id = _userId,
            FullName = "Time Traveler User",
            Email = "timetraveler@kopikala.com",
            PhoneNumber = "081234567890",
            PasswordHash = "hash",
            CreatedAt = DateTime.UtcNow
        });

        _context.DiningTables.Add(new DiningTable
        {
            Id = _tableId,
            TableNumber = "IN-01",
            Capacity = 4,
            Area = "Indoor AC",
            IsActive = true
        });

        _context.Timeslots.Add(new Timeslot
        {
            Id = 1,
            SessionName = "Sesi Pagi (09:00 - 11:00)",
            StartTime = new TimeOnly(9, 0),
            EndTime = new TimeOnly(11, 0)
        });

        _context.SaveChanges();
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    [Fact]
    public async Task Booking15MinExpiry_TimeTravelerAdvance16Min_ExpiresBookingAutomatically()
    {
        // 1. Arrange: Buat booking dengan status MenungguBayar pada T = 10:00:00
        var bookingDto = new CreateBookingRequestDto
        {
            TableId = _tableId,
            BookingDate = new DateOnly(2026, 9, 7),
            TimeslotId = 1,
            DurationHours = 2,
            RepresentativeName = "Traveler Alpha",
            CustomerPhone = "081234567890",
            PaymentMethod = "TransferBank"
        };

        var bookingId = await _bookingService.CreateBookingAsync(bookingDto, _userId);
        var initialInvoice = await _bookingService.GetInvoiceAsync(bookingId);

        _output.WriteLine($"[TIME TRAVEL] Booking Created: {initialInvoice!.CreatedAt:yyyy-MM-dd HH:mm:ss} UTC");
        _output.WriteLine($"[TIME TRAVEL] Booking Status Initial: {initialInvoice.Status}");
        _output.WriteLine($"[TIME TRAVEL] Expiration Limit: {initialInvoice.ExpiresAt:yyyy-MM-dd HH:mm:ss} UTC");

        Assert.Equal("MenungguBayar", initialInvoice.Status);

        // 2. Act 1: Majukan waktu 10 menit (masih dalam batas 15 menit)
        _fakeTime.Advance(TimeSpan.FromMinutes(10));
        _output.WriteLine($"[TIME TRAVEL] Advanced +10m -> Now: {_fakeTime.GetUtcNow():yyyy-MM-dd HH:mm:ss} UTC");

        var invoiceAt10m = await _bookingService.GetInvoiceAsync(bookingId);
        Assert.Equal("MenungguBayar", invoiceAt10m!.Status);
        _output.WriteLine($"[TIME TRAVEL] Status at +10m: {invoiceAt10m.Status} (Still Valid)");

        // 3. Act 2: Majukan waktu lagi 6 menit (Total +16m -> T = 10:16:00, Melebihi 15m)
        _fakeTime.Advance(TimeSpan.FromMinutes(6));
        _output.WriteLine($"[TIME TRAVEL] Advanced +6m (Total +16m) -> Now: {_fakeTime.GetUtcNow():yyyy-MM-dd HH:mm:ss} UTC");

        var invoiceAt16m = await _bookingService.GetInvoiceAsync(bookingId);

        // 4. Assert: Status otomatis berubah menjadi Kedaluwarsa
        Assert.Equal("Kedaluwarsa", invoiceAt16m!.Status);
        _output.WriteLine($"[TIME TRAVEL] Status at +16m: {invoiceAt16m.Status} (Successfully Expired)");

        // 5. Verifikasi meja kembali berstatus Tersedia (Available)
        var availableTables = await _bookingService.GetAvailableTablesAsync(new DateOnly(2026, 9, 7), 1);
        var table = availableTables.First(t => t.Id == _tableId);
        Assert.True(table.IsAvailable);
        _output.WriteLine("[SUCCESS] Meja otomatis kembali 'Available' setelah booking kedaluwarsa.");
    }
}
