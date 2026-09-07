using KopiKala.Data;
using KopiKala.DTOs.Booking;
using KopiKala.Helpers;
using KopiKala.Models;
using KopiKala.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;
using Moq;
using Xunit;

namespace KopiKala.Tests.Services;

public class BookingServiceTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly Mock<IWebHostEnvironment> _envMock;
    private readonly FakeTimeProvider _timeProvider;
    private readonly Mock<ILogger<BookingService>> _loggerMock;
    private readonly BookingService _sut;
    private readonly string _tempWebRoot;

    private readonly Guid _testUserId = Guid.NewGuid();
    private readonly Guid _table1Id = Guid.NewGuid();
    private readonly Guid _table2Id = Guid.NewGuid();
    private readonly Guid _menuItem1Id = Guid.NewGuid();
    private readonly Guid _menuItem2Id = Guid.NewGuid();

    public BookingServiceTests()
    {
        var dbName = Guid.NewGuid().ToString();
        var services = new ServiceCollection();
        services.AddDbContext<AppDbContext>(options =>
            options.UseInMemoryDatabase(databaseName: dbName));

        _serviceProvider = services.BuildServiceProvider();
        _scopeFactory = _serviceProvider.GetRequiredService<IServiceScopeFactory>();

        _envMock = new Mock<IWebHostEnvironment>();
        _tempWebRoot = Path.Combine(Path.GetTempPath(), "kopikala_test_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempWebRoot);
        _envMock.Setup(e => e.WebRootPath).Returns(_tempWebRoot);

        _timeProvider = new FakeTimeProvider();
        _timeProvider.SetUtcNow(new DateTimeOffset(2026, 9, 7, 10, 0, 0, TimeSpan.Zero));

        _loggerMock = new Mock<ILogger<BookingService>>();
        _sut = new BookingService(_scopeFactory, _envMock.Object, _timeProvider, _loggerMock.Object);

        SeedData();
    }

    private AppDbContext GetContext() => _serviceProvider.CreateScope().ServiceProvider.GetRequiredService<AppDbContext>();

    private void SeedData()
    {
        using var context = GetContext();
        var user = new User
        {
            Id = _testUserId,
            FullName = "Customer Test",
            Email = "customer@kopikala.com",
            PhoneNumber = "081234567890",
            PasswordHash = "hashed",
            CreatedAt = DateTime.UtcNow
        };
        context.Users.Add(user);

        var table1 = new DiningTable
        {
            Id = _table1Id,
            TableNumber = "IN-01",
            Capacity = 4,
            Area = "Indoor AC",
            IsActive = true
        };
        var table2 = new DiningTable
        {
            Id = _table2Id,
            TableNumber = "OUT-01",
            Capacity = 2,
            Area = "Outdoor Smoking",
            IsActive = true
        };
        context.DiningTables.AddRange(table1, table2);

        var timeslot1 = new Timeslot
        {
            Id = 1,
            SessionName = "Sesi Pagi (09:00 - 11:00)",
            StartTime = new TimeOnly(9, 0),
            EndTime = new TimeOnly(11, 0)
        };
        var timeslot2 = new Timeslot
        {
            Id = 2,
            SessionName = "Sesi Siang (11:00 - 13:00)",
            StartTime = new TimeOnly(11, 0),
            EndTime = new TimeOnly(13, 0)
        };
        context.Timeslots.AddRange(timeslot1, timeslot2);

        var menu1 = new MenuItem
        {
            Id = _menuItem1Id,
            Name = "Kopi Susu Gula Aren",
            Category = "Coffee",
            Price = 22000,
            Stock = 50,
            IsAvailable = true
        };
        var menu2 = new MenuItem
        {
            Id = _menuItem2Id,
            Name = "Butter Croissant",
            Category = "Pastry & Bakery",
            Price = 25000,
            Stock = 20,
            IsAvailable = true
        };
        var menuUnavailable = new MenuItem
        {
            Id = Guid.NewGuid(),
            Name = "Menu Habis",
            Category = "Coffee",
            Price = 30000,
            Stock = 0,
            IsAvailable = false
        };
        context.MenuItems.AddRange(menu1, menu2, menuUnavailable);

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
    public async Task GetTimeslotsAsync_ReturnsAllTimeslotsOrdered()
    {
        var result = await _sut.GetTimeslotsAsync();
        Assert.Equal(2, result.Count);
        Assert.Equal(1, result[0].Id);
        Assert.Equal(2, result[1].Id);
    }

    [Fact]
    public async Task GetAvailableMenuItemsAsync_ReturnsOnlyAvailableItems()
    {
        var result = await _sut.GetAvailableMenuItemsAsync();
        Assert.Equal(2, result.Count);
        Assert.DoesNotContain(result, m => !m.IsAvailable);
    }

    [Fact]
    public async Task GetAvailableTablesAsync_WithNoBookings_AllTablesAvailable()
    {
        var date = new DateOnly(2026, 9, 7);
        var tables = await _sut.GetAvailableTablesAsync(date, 1);

        Assert.Equal(2, tables.Count);
        Assert.All(tables, t => Assert.True(t.IsAvailable));
    }

    [Fact]
    public async Task GetAvailableTablesAsync_WithBookedTable_MarksBookedTableUnavailable()
    {
        var date = new DateOnly(2026, 9, 7);

        // Book table 1 for slot 1
        using (var context = GetContext())
        {
            var booking = new Booking
            {
                Id = Guid.NewGuid(),
                InvoiceCode = "INV/20260907/001",
                UserId = _testUserId,
                TableId = _table1Id,
                TimeslotId = 1,
                BookingDate = date,
                DurationHours = 2,
                RepresentativeName = "John Doe",
                Status = "MenungguBayar",
                PaymentMethod = "TransferBank",
                TotalAmount = 50000,
                ExpiresAt = DateTime.UtcNow.AddMinutes(15),
                CreatedAt = DateTime.UtcNow
            };
            context.Bookings.Add(booking);
            await context.SaveChangesAsync();
        }

        var tables = await _sut.GetAvailableTablesAsync(date, 1);
        var table1 = tables.First(t => t.Id == _table1Id);
        var table2 = tables.First(t => t.Id == _table2Id);

        Assert.False(table1.IsAvailable);
        Assert.True(table2.IsAvailable);
    }

    [Fact]
    public async Task CreateBookingAsync_ValidRequest_CreatesBookingAndDetails()
    {
        var dto = new CreateBookingRequestDto
        {
            TableId = _table1Id,
            BookingDate = new DateOnly(2026, 9, 7),
            TimeslotId = 1,
            DurationHours = 2,
            RepresentativeName = "Ivano Wisnu",
            CustomerPhone = "081234567890",
            PaymentMethod = "TransferBank",
            SelectedItems = new List<OrderItemDto>
            {
                new() { MenuItemId = _menuItem1Id, Quantity = 2 },
                new() { MenuItemId = _menuItem2Id, Quantity = 1 }
            }
        };

        var bookingId = await _sut.CreateBookingAsync(dto, _testUserId);
        Assert.NotEqual(Guid.Empty, bookingId);

        using var context = GetContext();
        var booking = await context.Bookings.Include(b => b.BookingDetails).FirstOrDefaultAsync(b => b.Id == bookingId);
        Assert.NotNull(booking);
        Assert.Equal("Ivano Wisnu", booking.RepresentativeName);
        Assert.Equal("MenungguBayar", booking.Status);
        Assert.Equal(2, booking.BookingDetails.Count);

        // Price: (2 * 22000) + (1 * 25000) = 44000 + 25000 = 69000
        Assert.Equal(69000, booking.TotalAmount);
    }

    [Fact]
    public async Task CreateBookingAsync_PastDate_ThrowsArgumentException()
    {
        var dto = new CreateBookingRequestDto
        {
            TableId = _table1Id,
            BookingDate = new DateOnly(2026, 9, 1), // Past date relative to fakeTime (2026-09-07)
            TimeslotId = 1,
            DurationHours = 2,
            RepresentativeName = "Test",
            CustomerPhone = "081234567890"
        };

        await Assert.ThrowsAsync<ArgumentException>(() => _sut.CreateBookingAsync(dto, _testUserId));
    }

    [Fact]
    public async Task CreateBookingAsync_InvalidDuration_ThrowsArgumentException()
    {
        var dto = new CreateBookingRequestDto
        {
            TableId = _table1Id,
            BookingDate = new DateOnly(2026, 9, 7),
            TimeslotId = 1,
            DurationHours = 5, // Invalid > 3
            RepresentativeName = "Test",
            CustomerPhone = "081234567890"
        };

        await Assert.ThrowsAsync<ArgumentException>(() => _sut.CreateBookingAsync(dto, _testUserId));
    }

    [Fact]
    public async Task CreateBookingAsync_AlreadyBooked_ThrowsInvalidOperationException()
    {
        var date = new DateOnly(2026, 9, 7);

        // Pre-existing booking
        using (var context = GetContext())
        {
            context.Bookings.Add(new Booking
            {
                Id = Guid.NewGuid(),
                InvoiceCode = "INV/20260907/002",
                UserId = _testUserId,
                TableId = _table1Id,
                TimeslotId = 1,
                BookingDate = date,
                DurationHours = 2,
                RepresentativeName = "Guest 1",
                Status = "MenungguBayar",
                PaymentMethod = "TransferBank",
                ExpiresAt = DateTime.UtcNow.AddMinutes(15),
                CreatedAt = DateTime.UtcNow
            });
            await context.SaveChangesAsync();
        }

        var dto = new CreateBookingRequestDto
        {
            TableId = _table1Id,
            BookingDate = date,
            TimeslotId = 1,
            DurationHours = 2,
            RepresentativeName = "Guest 2",
            CustomerPhone = "081234567891"
        };

        await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.CreateBookingAsync(dto, _testUserId));
    }

    [Fact]
    public async Task CreateBookingAsync_BayarDiTempat_SetsStatusDikonfirmasi()
    {
        var dto = new CreateBookingRequestDto
        {
            TableId = _table2Id,
            BookingDate = new DateOnly(2026, 9, 7),
            TimeslotId = 1,
            DurationHours = 2,
            RepresentativeName = "Walkin User",
            CustomerPhone = "081234567892",
            PaymentMethod = "BayarDiTempat"
        };

        var bookingId = await _sut.CreateBookingAsync(dto, _testUserId);
        using var context = GetContext();
        var booking = await context.Bookings.FindAsync(bookingId);

        Assert.NotNull(booking);
        Assert.Equal("Dikonfirmasi", booking.Status);
        Assert.Equal("BayarDiTempat", booking.PaymentMethod);
    }

    [Fact]
    public async Task GetInvoiceAsync_CalculatesCorrectEndTimeAndTotals()
    {
        var dto = new CreateBookingRequestDto
        {
            TableId = _table1Id,
            BookingDate = new DateOnly(2026, 9, 7),
            TimeslotId = 1, // StartTime 09:00
            DurationHours = 3,
            RepresentativeName = "Family VIP",
            CustomerPhone = "081234567890",
            PaymentMethod = "TransferBank",
            SelectedItems = new List<OrderItemDto>
            {
                new() { MenuItemId = _menuItem1Id, Quantity = 3 }
            }
        };

        var bookingId = await _sut.CreateBookingAsync(dto, _testUserId);
        var invoice = await _sut.GetInvoiceAsync(bookingId);

        Assert.NotNull(invoice);
        Assert.Equal("IN-01", invoice.TableNumber);
        Assert.Equal(TimeSpan.FromHours(9), invoice.StartTime);
        Assert.Equal(TimeSpan.FromHours(12), invoice.EndTime); // 9 + 3 = 12
        Assert.Equal(3, invoice.DurationHours);
        Assert.Equal(66000, invoice.TotalAmount); // 3 * 22000
    }

    [Fact]
    public async Task UploadPaymentProofAsync_ValidJpeg_SavesAndUpdatesStatus()
    {
        var dto = new CreateBookingRequestDto
        {
            TableId = _table1Id,
            BookingDate = new DateOnly(2026, 9, 7),
            TimeslotId = 1,
            DurationHours = 2,
            RepresentativeName = "Transfer User",
            PaymentMethod = "TransferBank"
        };
        var bookingId = await _sut.CreateBookingAsync(dto, _testUserId);

        var validJpegBytes = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46 };
        using var stream = new MemoryStream(validJpegBytes);

        var success = await _sut.UploadPaymentProofAsync(bookingId, stream, "transfer_struk.jpg");
        Assert.True(success);

        var invoice = await _sut.GetInvoiceAsync(bookingId);
        Assert.NotNull(invoice);
        Assert.Equal("MenungguVerifikasiKasir", invoice.Status);
        Assert.NotNull(invoice.PaymentProofUrl);
        Assert.StartsWith("/uploads/payments/", invoice.PaymentProofUrl);
    }

    [Fact]
    public async Task UploadPaymentProofAsync_ValidPng_SavesSuccessfully()
    {
        var dto = new CreateBookingRequestDto
        {
            TableId = _table1Id,
            BookingDate = new DateOnly(2026, 9, 7),
            TimeslotId = 1,
            DurationHours = 2,
            RepresentativeName = "PNG User",
            PaymentMethod = "TransferBank"
        };
        var bookingId = await _sut.CreateBookingAsync(dto, _testUserId);

        var validPngBytes = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00 };
        using var stream = new MemoryStream(validPngBytes);

        var success = await _sut.UploadPaymentProofAsync(bookingId, stream, "receipt.png");
        Assert.True(success);
    }

    [Fact]
    public async Task UploadPaymentProofAsync_InvalidExtension_ThrowsArgumentException()
    {
        var dto = new CreateBookingRequestDto
        {
            TableId = _table1Id,
            BookingDate = new DateOnly(2026, 9, 7),
            TimeslotId = 1,
            DurationHours = 2,
            RepresentativeName = "PDF User",
            PaymentMethod = "TransferBank"
        };
        var bookingId = await _sut.CreateBookingAsync(dto, _testUserId);

        var pdfBytes = new byte[] { 0x25, 0x50, 0x44, 0x46 };
        using var stream = new MemoryStream(pdfBytes);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            _sut.UploadPaymentProofAsync(bookingId, stream, "receipt.pdf"));
    }

    [Fact]
    public async Task GetUserBookingsAsync_ReturnsAllUserBookings()
    {
        var date = new DateOnly(2026, 9, 7);

        using (var context = GetContext())
        {
            context.Bookings.Add(new Booking
            {
                Id = Guid.NewGuid(),
                InvoiceCode = "INV/20260907/U1",
                UserId = _testUserId,
                TableId = _table1Id,
                TimeslotId = 1,
                BookingDate = date,
                DurationHours = 2,
                RepresentativeName = "Ivano",
                Status = "Dikonfirmasi",
                PaymentMethod = "BayarDiTempat",
                TotalAmount = 44000,
                ExpiresAt = DateTime.UtcNow.AddHours(2),
                CreatedAt = DateTime.UtcNow
            });
            await context.SaveChangesAsync();
        }

        var list = await _sut.GetUserBookingsAsync(_testUserId);
        Assert.NotEmpty(list);
        Assert.Contains(list, b => b.InvoiceCode == "INV/20260907/U1");
    }
}
