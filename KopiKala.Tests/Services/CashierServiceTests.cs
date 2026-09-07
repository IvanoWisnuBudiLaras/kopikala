using KopiKala.Data;
using KopiKala.DTOs.Booking;
using KopiKala.DTOs.Staff;
using KopiKala.Models;
using KopiKala.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;
using Moq;
using Xunit;

namespace KopiKala.Tests.Services;

public class CashierServiceTests
{
    private readonly AppDbContext _context;
    private readonly FakeTimeProvider _timeProvider;
    private readonly Mock<ILogger<CashierService>> _loggerMock;
    private readonly CashierService _sut;

    private readonly Guid _cashierId = Guid.NewGuid();
    private readonly Guid _customerUserId = Guid.NewGuid();
    private readonly Guid _table1Id = Guid.NewGuid();
    private readonly Guid _table2Id = Guid.NewGuid();
    private readonly Guid _menuItem1Id = Guid.NewGuid();
    private readonly Guid _menuItem2Id = Guid.NewGuid();

    public CashierServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new AppDbContext(options);
        _timeProvider = new FakeTimeProvider();
        _timeProvider.SetUtcNow(new DateTimeOffset(2026, 9, 7, 10, 0, 0, TimeSpan.Zero));
        _loggerMock = new Mock<ILogger<CashierService>>();

        _sut = new CashierService(_context, _timeProvider, _loggerMock.Object);

        SeedData();
    }

    private void SeedData()
    {
        var cashier = new User
        {
            Id = _cashierId,
            FullName = "Kasir Test",
            Email = "kasir@kopikala.com",
            PhoneNumber = "08111111111",
            PasswordHash = "hash",
            CreatedAt = DateTime.UtcNow
        };

        var customer = new User
        {
            Id = _customerUserId,
            FullName = "Budi Customer",
            Email = "budi@kopikala.com",
            PhoneNumber = "081234567890",
            PasswordHash = "hash",
            CreatedAt = DateTime.UtcNow
        };

        var table1 = new DiningTable
        {
            Id = _table1Id,
            TableNumber = "T-01",
            Capacity = 4,
            Area = "Indoor",
            IsActive = true
        };

        var table2 = new DiningTable
        {
            Id = _table2Id,
            TableNumber = "T-02",
            Capacity = 2,
            Area = "Outdoor",
            IsActive = true
        };

        var ts1 = new Timeslot
        {
            Id = 1,
            SessionName = "Sesi 1 (Pagi)",
            StartTime = new TimeOnly(10, 0),
            EndTime = new TimeOnly(12, 0)
        };

        var ts2 = new Timeslot
        {
            Id = 2,
            SessionName = "Sesi 2 (Siang)",
            StartTime = new TimeOnly(12, 0),
            EndTime = new TimeOnly(14, 0)
        };

        var menu1 = new MenuItem
        {
            Id = _menuItem1Id,
            Name = "Kopi Susu Gula Aren",
            Category = "Coffee",
            Price = 20000,
            Stock = 50,
            IsAvailable = true
        };

        var menu2 = new MenuItem
        {
            Id = _menuItem2Id,
            Name = "Americano",
            Category = "Coffee",
            Price = 25000,
            Stock = 50,
            IsAvailable = true
        };

        _context.Users.AddRange(cashier, customer);
        _context.DiningTables.AddRange(table1, table2);
        _context.Timeslots.AddRange(ts1, ts2);
        _context.MenuItems.AddRange(menu1, menu2);
        _context.SaveChanges();
    }

    [Fact]
    public async Task GetTableStatusesAsync_ReturnsCorrectStatusesForBookedAndFreeTables()
    {
        var date = DateOnly.FromDateTime(_timeProvider.GetUtcNow().LocalDateTime);

        var booking = new Booking
        {
            Id = Guid.NewGuid(),
            InvoiceCode = "INV/20260907/001",
            UserId = _customerUserId,
            TableId = _table1Id,
            TimeslotId = 1,
            BookingDate = date,
            DurationHours = 2,
            RepresentativeName = "Kak Budi",
            PaymentMethod = "TransferBank",
            Status = "SedangDigunakan",
            TotalAmount = 40000,
            ExpiresAt = DateTime.UtcNow.AddHours(2),
            CreatedAt = DateTime.UtcNow
        };
        _context.Bookings.Add(booking);
        await _context.SaveChangesAsync();

        var statuses = await _sut.GetTableStatusesAsync(date, 1);

        Assert.Equal(2, statuses.Count);

        var table1Status = statuses.First(s => s.TableId == _table1Id);
        Assert.Equal("SedangDigunakan", table1Status.DisplayStatus);
        Assert.Equal("Kak Budi", table1Status.CurrentRepresentativeName);
        Assert.Equal(40000, table1Status.TotalAmount);

        var table2Status = statuses.First(s => s.TableId == _table2Id);
        Assert.Equal("Tersedia", table2Status.DisplayStatus);
        Assert.Null(table2Status.CurrentBookingId);
    }

    [Fact]
    public async Task GetPendingVerificationsAsync_ReturnsOnlyMenungguVerifikasiKasir()
    {
        var date = DateOnly.FromDateTime(_timeProvider.GetUtcNow().LocalDateTime);

        var b1 = new Booking
        {
            Id = Guid.NewGuid(),
            InvoiceCode = "INV/20260907/PV1",
            UserId = _customerUserId,
            TableId = _table1Id,
            TimeslotId = 1,
            BookingDate = date,
            DurationHours = 2,
            RepresentativeName = "Verif User",
            PaymentMethod = "TransferBank",
            Status = "MenungguVerifikasiKasir",
            TotalAmount = 25000,
            ExpiresAt = DateTime.UtcNow.AddMinutes(15),
            CreatedAt = DateTime.UtcNow
        };

        var b2 = new Booking
        {
            Id = Guid.NewGuid(),
            InvoiceCode = "INV/20260907/PV2",
            UserId = _customerUserId,
            TableId = _table2Id,
            TimeslotId = 1,
            BookingDate = date,
            DurationHours = 2,
            RepresentativeName = "Confirmed User",
            PaymentMethod = "TransferBank",
            Status = "Dikonfirmasi",
            TotalAmount = 50000,
            ExpiresAt = DateTime.UtcNow.AddHours(2),
            CreatedAt = DateTime.UtcNow
        };

        _context.Bookings.AddRange(b1, b2);
        await _context.SaveChangesAsync();

        var pending = await _sut.GetPendingVerificationsAsync();

        Assert.Single(pending);
        Assert.Equal("INV/20260907/PV1", pending[0].InvoiceCode);
    }

    [Fact]
    public async Task VerifyPaymentAsync_ApprovedTrue_SetsDikonfirmasi()
    {
        var booking = new Booking
        {
            Id = Guid.NewGuid(),
            InvoiceCode = "INV/20260907/VER1",
            UserId = _customerUserId,
            TableId = _table1Id,
            TimeslotId = 1,
            BookingDate = DateOnly.FromDateTime(DateTime.Today),
            DurationHours = 2,
            RepresentativeName = "Test Verif",
            PaymentMethod = "TransferBank",
            Status = "MenungguVerifikasiKasir",
            TotalAmount = 20000,
            ExpiresAt = DateTime.UtcNow.AddMinutes(15),
            CreatedAt = DateTime.UtcNow
        };
        _context.Bookings.Add(booking);
        await _context.SaveChangesAsync();

        var result = await _sut.VerifyPaymentAsync(booking.Id, true);

        Assert.True(result);
        var updated = await _context.Bookings.FindAsync(booking.Id);
        Assert.NotNull(updated);
        Assert.Equal("Dikonfirmasi", updated.Status);
    }

    [Fact]
    public async Task VerifyPaymentAsync_ApprovedFalse_SetsBatal()
    {
        var booking = new Booking
        {
            Id = Guid.NewGuid(),
            InvoiceCode = "INV/20260907/VER2",
            UserId = _customerUserId,
            TableId = _table1Id,
            TimeslotId = 1,
            BookingDate = DateOnly.FromDateTime(DateTime.Today),
            DurationHours = 2,
            RepresentativeName = "Test Reject",
            PaymentMethod = "TransferBank",
            Status = "MenungguVerifikasiKasir",
            TotalAmount = 20000,
            ExpiresAt = DateTime.UtcNow.AddMinutes(15),
            CreatedAt = DateTime.UtcNow
        };
        _context.Bookings.Add(booking);
        await _context.SaveChangesAsync();

        var result = await _sut.VerifyPaymentAsync(booking.Id, false);

        Assert.True(result);
        var updated = await _context.Bookings.FindAsync(booking.Id);
        Assert.NotNull(updated);
        Assert.Equal("Batal", updated.Status);
    }

    [Fact]
    public async Task CheckInGuestAsync_ValidBooking_SetsSedangDigunakanAndSeatedAt()
    {
        var booking = new Booking
        {
            Id = Guid.NewGuid(),
            InvoiceCode = "INV/20260907/CHK1",
            UserId = _customerUserId,
            TableId = _table1Id,
            TimeslotId = 1,
            BookingDate = DateOnly.FromDateTime(DateTime.Today),
            DurationHours = 2,
            RepresentativeName = "Test Checkin",
            PaymentMethod = "TransferBank",
            Status = "Dikonfirmasi",
            TotalAmount = 20000,
            ExpiresAt = DateTime.UtcNow.AddHours(2),
            CreatedAt = DateTime.UtcNow
        };
        _context.Bookings.Add(booking);
        await _context.SaveChangesAsync();

        var result = await _sut.CheckInGuestAsync(booking.Id);

        Assert.True(result);
        var updated = await _context.Bookings.FindAsync(booking.Id);
        Assert.NotNull(updated);
        Assert.Equal("SedangDigunakan", updated.Status);
        Assert.NotNull(updated.SeatedAt);
    }

    [Fact]
    public async Task CheckInGuestAsync_InvalidStatus_ThrowsInvalidOperationException()
    {
        var booking = new Booking
        {
            Id = Guid.NewGuid(),
            InvoiceCode = "INV/20260907/CHK2",
            UserId = _customerUserId,
            TableId = _table1Id,
            TimeslotId = 1,
            BookingDate = DateOnly.FromDateTime(DateTime.Today),
            DurationHours = 2,
            RepresentativeName = "Test Batal",
            PaymentMethod = "TransferBank",
            Status = "Batal",
            TotalAmount = 20000,
            ExpiresAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };
        _context.Bookings.Add(booking);
        await _context.SaveChangesAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.CheckInGuestAsync(booking.Id));
    }

    [Fact]
    public async Task CreateWalkInBookingAsync_ValidRequest_CreatesAndSeatsGuest()
    {
        var dto = new WalkInBookingRequestDto
        {
            TableId = _table2Id,
            TimeslotId = 1,
            DurationHours = 2,
            RepresentativeName = "Walkin Guest",
            CustomerPhone = "0899999999",
            PaymentMethod = "Tunai",
            SelectedItems = new List<OrderItemDto>
            {
                new() { MenuItemId = _menuItem1Id, Quantity = 2, UnitPrice = 20000 }
            }
        };

        var bookingId = await _sut.CreateWalkInBookingAsync(dto, _cashierId);

        Assert.NotEqual(Guid.Empty, bookingId);
        var created = await _context.Bookings.Include(b => b.BookingDetails).FirstOrDefaultAsync(b => b.Id == bookingId);
        Assert.NotNull(created);
        Assert.Equal("SedangDigunakan", created.Status);
        Assert.Equal("Walkin Guest", created.RepresentativeName);
        Assert.Equal(40000, created.TotalAmount);
        Assert.Single(created.BookingDetails);
    }

    [Fact]
    public async Task CreateWalkInBookingAsync_AlreadyOccupiedTable_ThrowsInvalidOperationException()
    {
        var date = DateOnly.FromDateTime(_timeProvider.GetUtcNow().LocalDateTime);

        var existing = new Booking
        {
            Id = Guid.NewGuid(),
            InvoiceCode = "INV/20260907/OCC1",
            UserId = _customerUserId,
            TableId = _table1Id,
            TimeslotId = 1,
            BookingDate = date,
            DurationHours = 2,
            RepresentativeName = "Existing Guest",
            PaymentMethod = "BayarDiTempat",
            Status = "Dikonfirmasi",
            TotalAmount = 20000,
            ExpiresAt = DateTime.UtcNow.AddHours(2),
            CreatedAt = DateTime.UtcNow
        };
        _context.Bookings.Add(existing);
        await _context.SaveChangesAsync();

        var dto = new WalkInBookingRequestDto
        {
            TableId = _table1Id,
            TimeslotId = 1,
            DurationHours = 2,
            RepresentativeName = "New Guest",
            PaymentMethod = "Tunai"
        };

        await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.CreateWalkInBookingAsync(dto, _cashierId));
    }

    [Fact]
    public async Task AddOrderToActiveBookingAsync_ValidItems_AddsAddOnDetailsAndIncreasesTotal()
    {
        var booking = new Booking
        {
            Id = Guid.NewGuid(),
            InvoiceCode = "INV/20260907/ADD1",
            UserId = _customerUserId,
            TableId = _table1Id,
            TimeslotId = 1,
            BookingDate = DateOnly.FromDateTime(DateTime.Today),
            DurationHours = 2,
            RepresentativeName = "Active Guest",
            PaymentMethod = "Tunai",
            Status = "SedangDigunakan",
            TotalAmount = 20000,
            ExpiresAt = DateTime.UtcNow.AddHours(2),
            CreatedAt = DateTime.UtcNow
        };
        _context.Bookings.Add(booking);
        await _context.SaveChangesAsync();

        var addOnDto = new AddOnOrderRequestDto
        {
            BookingId = booking.Id,
            Items = new List<OrderItemDto>
            {
                new() { MenuItemId = _menuItem2Id, Quantity = 2, UnitPrice = 25000 }
            }
        };

        var result = await _sut.AddOrderToActiveBookingAsync(addOnDto);

        Assert.True(result);
        var updated = await _context.Bookings.Include(b => b.BookingDetails).FirstOrDefaultAsync(b => b.Id == booking.Id);
        Assert.NotNull(updated);
        Assert.Equal(70000, updated.TotalAmount);
        Assert.Contains(updated.BookingDetails, d => d.OrderType == "AddOn" && d.MenuItemId == _menuItem2Id);
    }

    [Fact]
    public async Task SubstituteMenuItemAsync_ValidSubstitution_RecalculatesPriceDifference()
    {
        var detailId = Guid.NewGuid();
        var booking = new Booking
        {
            Id = Guid.NewGuid(),
            InvoiceCode = "INV/20260907/SUB1",
            UserId = _customerUserId,
            TableId = _table1Id,
            TimeslotId = 1,
            BookingDate = DateOnly.FromDateTime(DateTime.Today),
            DurationHours = 2,
            RepresentativeName = "Swap Guest",
            PaymentMethod = "Tunai",
            Status = "SedangDigunakan",
            TotalAmount = 20000,
            ExpiresAt = DateTime.UtcNow.AddHours(2),
            CreatedAt = DateTime.UtcNow,
            BookingDetails = new List<BookingDetail>
            {
                new()
                {
                    Id = detailId,
                    MenuItemId = _menuItem1Id,
                    Quantity = 1,
                    UnitPrice = 20000,
                    SubTotal = 20000,
                    OrderType = "PreOrder"
                }
            }
        };
        _context.Bookings.Add(booking);
        await _context.SaveChangesAsync();

        var subDto = new SubstituteItemRequestDto
        {
            BookingId = booking.Id,
            OldDetailId = detailId,
            NewMenuItemId = _menuItem2Id, // Price 25000 (+5000 diff)
            NewQuantity = 1
        };

        var result = await _sut.SubstituteMenuItemAsync(subDto);

        Assert.True(result);
        var updated = await _context.Bookings.Include(b => b.BookingDetails).FirstOrDefaultAsync(b => b.Id == booking.Id);
        Assert.NotNull(updated);
        Assert.Equal(25000, updated.TotalAmount);
        Assert.Equal(_menuItem2Id, updated.BookingDetails.First().MenuItemId);
    }

    [Fact]
    public async Task ExtendBookingDurationAsync_ValidRequest_ExtendsDuration()
    {
        var booking = new Booking
        {
            Id = Guid.NewGuid(),
            InvoiceCode = "INV/20260907/EXT1",
            UserId = _customerUserId,
            TableId = _table1Id,
            TimeslotId = 1,
            BookingDate = DateOnly.FromDateTime(DateTime.Today),
            DurationHours = 1,
            RepresentativeName = "Extend Guest",
            PaymentMethod = "Tunai",
            Status = "SedangDigunakan",
            TotalAmount = 20000,
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            CreatedAt = DateTime.UtcNow
        };
        _context.Bookings.Add(booking);
        await _context.SaveChangesAsync();

        var extendDto = new ExtendDurationRequestDto
        {
            BookingId = booking.Id,
            AdditionalHours = 1
        };

        var result = await _sut.ExtendBookingDurationAsync(extendDto);

        Assert.True(result);
        var updated = await _context.Bookings.FindAsync(booking.Id);
        Assert.NotNull(updated);
        Assert.Equal(2, updated.DurationHours);
    }

    [Fact]
    public async Task CompleteBookingSessionAsync_ValidSeated_SetsSelesai()
    {
        var booking = new Booking
        {
            Id = Guid.NewGuid(),
            InvoiceCode = "INV/20260907/DONE1",
            UserId = _customerUserId,
            TableId = _table1Id,
            TimeslotId = 1,
            BookingDate = DateOnly.FromDateTime(DateTime.Today),
            DurationHours = 2,
            RepresentativeName = "Done Guest",
            PaymentMethod = "Tunai",
            Status = "SedangDigunakan",
            TotalAmount = 20000,
            ExpiresAt = DateTime.UtcNow.AddHours(2),
            CreatedAt = DateTime.UtcNow
        };
        _context.Bookings.Add(booking);
        await _context.SaveChangesAsync();

        var result = await _sut.CompleteBookingSessionAsync(booking.Id);

        Assert.True(result);
        var updated = await _context.Bookings.FindAsync(booking.Id);
        Assert.NotNull(updated);
        Assert.Equal("Selesai", updated.Status);
    }

    [Fact]
    public async Task SearchBookingsAsync_ByQuery_ReturnsMatchingResults()
    {
        var booking = new Booking
        {
            Id = Guid.NewGuid(),
            InvoiceCode = "INV/20260907/SRCH99",
            UserId = _customerUserId,
            TableId = _table1Id,
            TimeslotId = 1,
            BookingDate = DateOnly.FromDateTime(DateTime.Today),
            DurationHours = 2,
            RepresentativeName = "Dimas Anggara",
            PaymentMethod = "Tunai",
            Status = "Dikonfirmasi",
            TotalAmount = 20000,
            ExpiresAt = DateTime.UtcNow.AddHours(2),
            CreatedAt = DateTime.UtcNow
        };
        _context.Bookings.Add(booking);
        await _context.SaveChangesAsync();

        var searchByName = await _sut.SearchBookingsAsync("dimas");
        Assert.Single(searchByName);
        Assert.Equal("INV/20260907/SRCH99", searchByName[0].InvoiceCode);

        var searchByInv = await _sut.SearchBookingsAsync("SRCH99");
        Assert.Single(searchByInv);

        var searchNotFound = await _sut.SearchBookingsAsync("NonExistentName123");
        Assert.Empty(searchNotFound);
    }
}
