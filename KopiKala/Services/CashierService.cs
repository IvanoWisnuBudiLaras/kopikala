using KopiKala.Data;
using KopiKala.DTOs.Booking;
using KopiKala.DTOs.Staff;
using KopiKala.Helpers;
using KopiKala.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace KopiKala.Services;

public class CashierService : ICashierService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<CashierService> _logger;

    public CashierService(IServiceScopeFactory scopeFactory, TimeProvider timeProvider, ILogger<CashierService> logger)
    {
        _scopeFactory = scopeFactory;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task<List<CashierTableStatusDto>> GetTableStatusesAsync(DateOnly date, int timeslotId)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var tables = await context.DiningTables
            .Where(t => t.IsActive)
            .OrderBy(t => t.TableNumber)
            .ToListAsync();

        var activeBookings = await context.Bookings
            .Include(b => b.Timeslot)
            .Where(b => b.BookingDate == date &&
                        b.TimeslotId == timeslotId &&
                        b.Status != "Batal" &&
                        b.Status != "NoShow" &&
                        b.Status != "Kedaluwarsa" &&
                        b.Status != "Selesai")
            .ToListAsync();

        var bookingMap = activeBookings.ToDictionary(b => b.TableId, b => b);

        var result = new List<CashierTableStatusDto>();
        foreach (var table in tables)
        {
            if (bookingMap.TryGetValue(table.Id, out var booking))
            {
                var endTime = booking.Timeslot.StartTime.AddHours(booking.DurationHours);
                result.Add(new CashierTableStatusDto
                {
                    TableId = table.Id,
                    TableNumber = table.TableNumber,
                    Capacity = table.Capacity,
                    Area = table.Area,
                    DisplayStatus = booking.Status switch
                    {
                        "SedangDigunakan" => "SedangDigunakan",
                        "MenungguVerifikasiKasir" => "MenungguVerifikasi",
                        "MenungguBayar" => "MenungguBayar",
                        _ => "Dikonfirmasi"
                    },
                    CurrentBookingId = booking.Id,
                    CurrentInvoiceCode = booking.InvoiceCode,
                    CurrentRepresentativeName = booking.RepresentativeName,
                    StartTime = booking.Timeslot.StartTime,
                    EndTime = endTime,
                    SeatedAt = booking.SeatedAt,
                    TotalAmount = booking.TotalAmount
                });
            }
            else
            {
                result.Add(new CashierTableStatusDto
                {
                    TableId = table.Id,
                    TableNumber = table.TableNumber,
                    Capacity = table.Capacity,
                    Area = table.Area,
                    DisplayStatus = "Tersedia",
                    CurrentBookingId = null,
                    CurrentInvoiceCode = null,
                    CurrentRepresentativeName = null,
                    StartTime = null,
                    EndTime = null,
                    SeatedAt = null,
                    TotalAmount = 0
                });
            }
        }

        return result;
    }

    public async Task<List<CashierBookingDto>> GetPendingVerificationsAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var bookings = await context.Bookings
            .Include(b => b.Table)
            .Include(b => b.Timeslot)
            .Include(b => b.User)
            .Include(b => b.BookingDetails)
                .ThenInclude(d => d.MenuItem)
            .Where(b => b.Status == "MenungguVerifikasiKasir")
            .OrderBy(b => b.CreatedAt)
            .ToListAsync();

        return bookings.Select(MapToDto).ToList();
    }

    public async Task<List<CashierBookingDto>> SearchBookingsAsync(string? query = null, DateOnly? date = null)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var q = context.Bookings
            .Include(b => b.Table)
            .Include(b => b.Timeslot)
            .Include(b => b.User)
            .Include(b => b.BookingDetails)
                .ThenInclude(d => d.MenuItem)
            .AsQueryable();

        if (date.HasValue)
        {
            q = q.Where(b => b.BookingDate == date.Value);
        }

        if (!string.IsNullOrWhiteSpace(query))
        {
            var clean = query.Trim().ToLower();
            q = q.Where(b =>
                b.InvoiceCode.ToLower().Contains(clean) ||
                b.RepresentativeName.ToLower().Contains(clean) ||
                b.Table.TableNumber.ToLower().Contains(clean) ||
                (b.User.PhoneNumber != null && b.User.PhoneNumber.Contains(clean)));
        }

        var bookings = await q.OrderByDescending(b => b.CreatedAt).Take(50).ToListAsync();
        return bookings.Select(MapToDto).ToList();
    }

    public async Task<CashierBookingDto?> GetBookingDetailsAsync(Guid bookingId)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var booking = await context.Bookings
            .Include(b => b.Table)
            .Include(b => b.Timeslot)
            .Include(b => b.User)
            .Include(b => b.BookingDetails)
                .ThenInclude(d => d.MenuItem)
            .FirstOrDefaultAsync(b => b.Id == bookingId);

        return booking == null ? null : MapToDto(booking);
    }

    public async Task<bool> VerifyPaymentAsync(Guid bookingId, bool approved, string? notes = null)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var booking = await context.Bookings.FirstOrDefaultAsync(b => b.Id == bookingId);
        if (booking == null) return false;

        booking.Status = approved ? "Dikonfirmasi" : "Batal";
        await context.SaveChangesAsync();

        _logger.LogInformation("Payment verification for Booking {BookingId}: Approved={Approved}", bookingId, approved);
        return true;
    }

    public async Task<bool> CheckInGuestAsync(Guid bookingId)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var booking = await context.Bookings.FirstOrDefaultAsync(b => b.Id == bookingId);
        if (booking == null) return false;

        if (booking.Status != "Dikonfirmasi" && booking.Status != "MenungguVerifikasiKasir")
        {
            throw new InvalidOperationException($"Status booking saat ini ({booking.Status}) tidak dapat di-check-in.");
        }

        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;
        booking.Status = "SedangDigunakan";
        booking.SeatedAt = nowUtc;

        await context.SaveChangesAsync();
        _logger.LogInformation("Guest checked in for Booking {BookingId} at {Time}", bookingId, nowUtc);
        return true;
    }

    public async Task<Guid> CreateWalkInBookingAsync(WalkInBookingRequestDto dto, Guid cashierId)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var today = DateOnly.FromDateTime(_timeProvider.GetUtcNow().LocalDateTime);

        var table = await context.DiningTables.FirstOrDefaultAsync(t => t.Id == dto.TableId && t.IsActive)
            ?? throw new InvalidOperationException("Meja tidak valid atau tidak aktif.");

        var timeslot = await context.Timeslots.FirstOrDefaultAsync(ts => ts.Id == dto.TimeslotId)
            ?? throw new InvalidOperationException("Sesi waktu tidak valid.");

        var isAlreadyBooked = await context.Bookings.AnyAsync(b =>
            b.TableId == dto.TableId &&
            b.BookingDate == today &&
            b.TimeslotId == dto.TimeslotId &&
            b.Status != "Batal" &&
            b.Status != "NoShow" &&
            b.Status != "Kedaluwarsa" &&
            b.Status != "Selesai");

        if (isAlreadyBooked)
        {
            throw new InvalidOperationException($"Meja {table.TableNumber} sudah terisi atau dipesan pada sesi ini.");
        }

        var itemIds = dto.SelectedItems.Select(x => x.MenuItemId).Distinct().ToList();
        var menuItems = await context.MenuItems
            .Where(m => itemIds.Contains(m.Id))
            .ToDictionaryAsync(m => m.Id, m => m);

        var details = new List<BookingDetail>();
        decimal totalAmount = 0;

        foreach (var item in dto.SelectedItems)
        {
            if (menuItems.TryGetValue(item.MenuItemId, out var menuItem))
            {
                var subTotal = item.Quantity * menuItem.Price;
                totalAmount += subTotal;
                details.Add(new BookingDetail
                {
                    Id = Guid.NewGuid(),
                    MenuItemId = menuItem.Id,
                    Quantity = item.Quantity,
                    UnitPrice = menuItem.Price,
                    SubTotal = subTotal,
                    OrderType = "PreOrder"
                });
            }
        }

        var cashier = await context.Users.FirstOrDefaultAsync(u => u.Id == cashierId)
            ?? await context.Users.FirstOrDefaultAsync(u => u.Email == "kasir@kopikala.com")
            ?? await context.Users.FirstAsync();
        var actualUserId = cashier.Id;

        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;
        var invoiceCode = InvoiceCodeHelper.GenerateInvoiceCode(today);

        var booking = new Booking
        {
            Id = Guid.NewGuid(),
            InvoiceCode = invoiceCode,
            UserId = actualUserId,
            TableId = dto.TableId,
            TimeslotId = dto.TimeslotId,
            BookingDate = today,
            DurationHours = dto.DurationHours,
            RepresentativeName = dto.RepresentativeName.Trim(),
            PaymentMethod = dto.PaymentMethod,
            Status = "SedangDigunakan",
            TotalAmount = totalAmount,
            SeatedAt = nowUtc,
            ExpiresAt = nowUtc.AddHours(dto.DurationHours),
            CreatedAt = nowUtc,
            BookingDetails = details
        };

        try
        {
            context.Bookings.Add(booking);
            await context.SaveChangesAsync();
            _logger.LogInformation("Walk-in booking created: {BookingId} for Table {TableNumber}", booking.Id, table.TableNumber);
            return booking.Id;
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "DbUpdateException on walk-in booking: {Inner}", ex.InnerException?.Message);
            throw new InvalidOperationException($"Gagal menyimpan booking: {ex.InnerException?.Message ?? ex.Message}", ex);
        }
    }

    public async Task<bool> AddOrderToActiveBookingAsync(AddOnOrderRequestDto dto)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var booking = await context.Bookings
            .Include(b => b.BookingDetails)
            .FirstOrDefaultAsync(b => b.Id == dto.BookingId);

        if (booking == null) return false;

        if (booking.Status != "SedangDigunakan" && booking.Status != "Dikonfirmasi")
        {
            throw new InvalidOperationException("Pesanan tambahan hanya dapat ditambahkan pada meja aktif.");
        }

        var itemIds = dto.Items.Select(x => x.MenuItemId).Distinct().ToList();
        var menuItems = await context.MenuItems
            .Where(m => itemIds.Contains(m.Id))
            .ToDictionaryAsync(m => m.Id, m => m);

        decimal addedAmount = 0;
        foreach (var item in dto.Items)
        {
            if (menuItems.TryGetValue(item.MenuItemId, out var menuItem))
            {
                var subTotal = item.Quantity * menuItem.Price;
                addedAmount += subTotal;
                var detail = new BookingDetail
                {
                    Id = Guid.NewGuid(),
                    BookingId = booking.Id,
                    MenuItemId = menuItem.Id,
                    Quantity = item.Quantity,
                    UnitPrice = menuItem.Price,
                    SubTotal = subTotal,
                    OrderType = "AddOn"
                };
                context.BookingDetails.Add(detail);
            }
        }

        booking.TotalAmount += addedAmount;
        await context.SaveChangesAsync();

        _logger.LogInformation("Add-on order added to Booking {BookingId}: +Rp{Amount}", booking.Id, addedAmount);
        return true;
    }

    public async Task<bool> SubstituteMenuItemAsync(SubstituteItemRequestDto dto)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var booking = await context.Bookings
            .Include(b => b.BookingDetails)
            .FirstOrDefaultAsync(b => b.Id == dto.BookingId);

        if (booking == null) return false;

        var oldDetail = booking.BookingDetails.FirstOrDefault(d => d.Id == dto.OldDetailId)
            ?? throw new InvalidOperationException("Item pesanan yang akan ditukar tidak ditemukan.");

        var newMenuItem = await context.MenuItems.FirstOrDefaultAsync(m => m.Id == dto.NewMenuItemId && m.IsAvailable)
            ?? throw new InvalidOperationException("Item menu pengganti tidak valid atau sedang habis.");

        var oldSubTotal = oldDetail.SubTotal;
        var newSubTotal = dto.NewQuantity * newMenuItem.Price;
        var priceDifference = newSubTotal - oldSubTotal;

        oldDetail.MenuItemId = newMenuItem.Id;
        oldDetail.Quantity = dto.NewQuantity;
        oldDetail.UnitPrice = newMenuItem.Price;
        oldDetail.SubTotal = newSubTotal;

        booking.TotalAmount += priceDifference;
        await context.SaveChangesAsync();

        _logger.LogInformation("Item substituted in Booking {BookingId}: Diff={Diff}", booking.Id, priceDifference);
        return true;
    }

    public async Task<bool> ExtendBookingDurationAsync(ExtendDurationRequestDto dto)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var booking = await context.Bookings
            .Include(b => b.Timeslot)
            .FirstOrDefaultAsync(b => b.Id == dto.BookingId);

        if (booking == null) return false;

        if (booking.Status != "SedangDigunakan")
        {
            throw new InvalidOperationException("Perpanjangan waktu hanya bisa dilakukan saat tamu sedang di meja.");
        }

        var nextTimeslot = await context.Timeslots
            .Where(ts => ts.StartTime >= booking.Timeslot.StartTime.AddHours(booking.DurationHours))
            .OrderBy(ts => ts.StartTime)
            .FirstOrDefaultAsync();

        if (nextTimeslot != null)
        {
            var isNextBooked = await context.Bookings.AnyAsync(b =>
                b.TableId == booking.TableId &&
                b.BookingDate == booking.BookingDate &&
                b.TimeslotId == nextTimeslot.Id &&
                b.Id != booking.Id &&
                b.Status != "Batal" &&
                b.Status != "NoShow" &&
                b.Status != "Kedaluwarsa" &&
                b.Status != "Selesai");

            if (isNextBooked)
            {
                throw new InvalidOperationException("Sesi waktu berikutnya untuk meja ini sudah dipesan pelanggan lain.");
            }
        }

        booking.DurationHours += dto.AdditionalHours;
        booking.ExpiresAt = booking.ExpiresAt.AddHours(dto.AdditionalHours);

        await context.SaveChangesAsync();
        _logger.LogInformation("Booking {BookingId} extended by {Hours} hours", booking.Id, dto.AdditionalHours);
        return true;
    }

    public async Task<bool> CompleteBookingSessionAsync(Guid bookingId)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var booking = await context.Bookings.FirstOrDefaultAsync(b => b.Id == bookingId);
        if (booking == null) return false;

        booking.Status = "Selesai";
        await context.SaveChangesAsync();

        _logger.LogInformation("Booking session completed: {BookingId}", bookingId);
        return true;
    }

    private static CashierBookingDto MapToDto(Booking b)
    {
        var endTime = b.Timeslot != null
            ? b.Timeslot.StartTime.AddHours(b.DurationHours)
            : TimeOnly.MinValue;

        return new CashierBookingDto
        {
            BookingId = b.Id,
            InvoiceCode = b.InvoiceCode,
            RepresentativeName = b.RepresentativeName,
            CustomerName = b.User?.FullName ?? b.RepresentativeName,
            CustomerPhone = b.User?.PhoneNumber ?? string.Empty,
            TableId = b.TableId,
            TableNumber = b.Table?.TableNumber ?? string.Empty,
            TableArea = b.Table?.Area ?? string.Empty,
            TableCapacity = b.Table?.Capacity ?? 0,
            TimeslotId = b.TimeslotId,
            TimeslotName = b.Timeslot?.SessionName ?? string.Empty,
            StartTime = b.Timeslot?.StartTime ?? TimeOnly.MinValue,
            EndTime = endTime,
            DurationHours = b.DurationHours,
            BookingDate = b.BookingDate,
            Status = b.Status,
            PaymentMethod = b.PaymentMethod,
            PaymentProofUrl = b.PaymentProofUrl,
            TotalAmount = b.TotalAmount,
            SeatedAt = b.SeatedAt,
            ExpiresAt = b.ExpiresAt,
            CreatedAt = b.CreatedAt,
            Items = b.BookingDetails.Select(d => new CashierOrderItemDto
            {
                DetailId = d.Id,
                MenuItemId = d.MenuItemId,
                MenuItemName = d.MenuItem?.Name ?? string.Empty,
                Quantity = d.Quantity,
                UnitPrice = d.UnitPrice,
                SubTotal = d.SubTotal,
                OrderType = d.OrderType
            }).ToList()
        };
    }
}
