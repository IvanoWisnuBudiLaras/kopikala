using KopiKala.Data;
using KopiKala.DTOs.Booking;
using KopiKala.Helpers;
using KopiKala.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace KopiKala.Services;

public class BookingService : IBookingService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IWebHostEnvironment _env;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<BookingService> _logger;

    public BookingService(
        IServiceScopeFactory scopeFactory,
        IWebHostEnvironment env,
        TimeProvider timeProvider,
        ILogger<BookingService> logger)
    {
        _scopeFactory = scopeFactory;
        _env = env;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task<List<DiningTableDto>> GetAvailableTablesAsync(DateOnly date, int timeslotId)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var tables = await context.DiningTables
            .Where(t => t.IsActive)
            .OrderBy(t => t.TableNumber)
            .ToListAsync();

        var bookedTableIds = await context.Bookings
            .Where(b => b.BookingDate == date &&
                        b.TimeslotId == timeslotId &&
                        b.Status != "Batal" &&
                        b.Status != "NoShow" &&
                        b.Status != "Kedaluwarsa")
            .Select(b => b.TableId)
            .Distinct()
            .ToListAsync();

        return tables.Select(t => new DiningTableDto
        {
            Id = t.Id,
            TableNumber = t.TableNumber,
            Capacity = t.Capacity,
            Area = t.Area,
            IsActive = t.IsActive,
            IsAvailable = !bookedTableIds.Contains(t.Id)
        }).ToList();
    }

    public async Task<List<TimeslotDto>> GetTimeslotsAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        return await context.Timeslots
            .OrderBy(ts => ts.StartTime)
            .Select(ts => new TimeslotDto
            {
                Id = ts.Id,
                SessionName = ts.SessionName,
                StartTime = ts.StartTime,
                EndTime = ts.EndTime
            })
            .ToListAsync();
    }

    public async Task<List<MenuItemDto>> GetAvailableMenuItemsAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        return await context.MenuItems
            .Where(m => m.IsAvailable)
            .OrderBy(m => m.Category)
            .ThenBy(m => m.Name)
            .Select(m => new MenuItemDto
            {
                Id = m.Id,
                Name = m.Name,
                Category = m.Category,
                Price = m.Price,
                Stock = m.Stock,
                ImageUrl = m.ImageUrl,
                IsAvailable = m.IsAvailable
            })
            .ToListAsync();
    }

    public async Task<Guid> CreateBookingAsync(CreateBookingRequestDto dto, Guid userId)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var today = DateOnly.FromDateTime(_timeProvider.GetUtcNow().LocalDateTime);
        if (dto.BookingDate < today)
        {
            throw new ArgumentException("Tanggal reservasi tidak boleh di masa lalu.");
        }

        var table = await context.DiningTables.FirstOrDefaultAsync(t => t.Id == dto.TableId && t.IsActive)
            ?? throw new InvalidOperationException("Meja yang dipilih tidak valid atau tidak aktif.");

        var timeslot = await context.Timeslots.FirstOrDefaultAsync(ts => ts.Id == dto.TimeslotId)
            ?? throw new InvalidOperationException("Sesi waktu yang dipilih tidak valid.");

        if (dto.DurationHours < 1 || dto.DurationHours > 3)
        {
            throw new ArgumentException("Durasi duduk harus antara 1 sampai 3 jam.");
        }

        // Check if table is already booked for this slot
        var isAlreadyBooked = await context.Bookings.AnyAsync(b =>
            b.TableId == dto.TableId &&
            b.BookingDate == dto.BookingDate &&
            b.TimeslotId == dto.TimeslotId &&
            b.Status != "Batal" &&
            b.Status != "NoShow" &&
            b.Status != "Kedaluwarsa");

        if (isAlreadyBooked)
        {
            throw new InvalidOperationException($"Meja {table.TableNumber} sudah terpesan pada sesi {timeslot.SessionName}. Silakan pilih meja atau sesi lain.");
        }

        // Calculate F&B items
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

        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;
        var invoiceCode = InvoiceCodeHelper.GenerateInvoiceCode(dto.BookingDate);

        var booking = new Booking
        {
            Id = Guid.NewGuid(),
            InvoiceCode = invoiceCode,
            UserId = userId,
            TableId = dto.TableId,
            TimeslotId = dto.TimeslotId,
            BookingDate = dto.BookingDate,
            DurationHours = dto.DurationHours,
            RepresentativeName = dto.RepresentativeName.Trim(),
            PaymentMethod = dto.PaymentMethod,
            Status = dto.PaymentMethod == "BayarDiTempat" ? "Dikonfirmasi" : "MenungguBayar",
            TotalAmount = totalAmount,
            ExpiresAt = nowUtc.AddMinutes(15),
            CreatedAt = nowUtc,
            BookingDetails = details
        };

        try
        {
            context.Bookings.Add(booking);
            await context.SaveChangesAsync();
            _logger.LogInformation("Booking created successfully with ID: {BookingId}, Code: {InvoiceCode}", booking.Id, booking.InvoiceCode);
            return booking.Id;
        }
        catch (DbUpdateException ex)
        {
            _logger.LogWarning(ex, "Concurrency conflict creating booking for Table {TableId} on {BookingDate} slot {TimeslotId}", dto.TableId, dto.BookingDate, dto.TimeslotId);
            throw new InvalidOperationException($"Meja {table.TableNumber} baru saja dipesan oleh pelanggan lain. Silakan pilih meja atau sesi lain.", ex);
        }
    }

    public async Task<InvoiceResponseDto?> GetInvoiceAsync(Guid bookingId)
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

        if (booking == null) return null;

        await CheckAndExpireBookingInternalAsync(booking, context);

        return MapToInvoiceDto(booking);
    }

    public async Task<InvoiceResponseDto?> GetInvoiceByCodeAsync(string invoiceCode)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var booking = await context.Bookings
            .Include(b => b.Table)
            .Include(b => b.Timeslot)
            .Include(b => b.User)
            .Include(b => b.BookingDetails)
                .ThenInclude(d => d.MenuItem)
            .FirstOrDefaultAsync(b => b.InvoiceCode == invoiceCode);

        if (booking == null) return null;

        await CheckAndExpireBookingInternalAsync(booking, context);

        return MapToInvoiceDto(booking);
    }

    public async Task<bool> UploadPaymentProofAsync(Guid bookingId, Stream fileStream, string fileName)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var booking = await context.Bookings.FirstOrDefaultAsync(b => b.Id == bookingId);
        if (booking == null) return false;

        if (booking.Status is "Batal" or "Kedaluwarsa" or "Lunas")
            return false;

        if (!FileSecurityHelper.IsValidImageExtension(fileName))
            throw new ArgumentException("Format file tidak didukung. Harap unggah file .jpg, .jpeg, atau .png.");

        using var memoryStream = new MemoryStream();
        await fileStream.CopyToAsync(memoryStream);
        var bytes = memoryStream.ToArray();

        if (bytes.Length > FileSecurityHelper.MaxFileSizeBytes)
            throw new ArgumentException("Ukuran file melebihi batas maksimal 2 MB.");

        if (!FileSecurityHelper.ValidateMagicBytes(bytes, fileName))
            throw new ArgumentException("File gambar tidak valid atau rusak.");

        var safeFileName = FileSecurityHelper.GenerateSafeFileName(fileName);
        var uploadsDir = Path.Combine(_env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"), "uploads", "payments");

        if (!Directory.Exists(uploadsDir))
        {
            Directory.CreateDirectory(uploadsDir);
        }

        var filePath = Path.Combine(uploadsDir, safeFileName);
        await File.WriteAllBytesAsync(filePath, bytes);

        booking.PaymentProofUrl = $"/uploads/payments/{safeFileName}";
        booking.Status = "MenungguVerifikasiKasir";
        await context.SaveChangesAsync();

        _logger.LogInformation("Payment proof uploaded for Booking: {BookingId}, File: {FileName}", bookingId, safeFileName);
        return true;
    }

    public async Task<bool> ConfirmPayOnSiteAsync(Guid bookingId)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var booking = await context.Bookings.FirstOrDefaultAsync(b => b.Id == bookingId);
        if (booking == null) return false;

        booking.PaymentMethod = "BayarDiTempat";
        booking.Status = "Dikonfirmasi";
        await context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> CheckAndExpireBookingAsync(Guid bookingId)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var booking = await context.Bookings.FirstOrDefaultAsync(b => b.Id == bookingId);
        if (booking == null) return false;

        return await CheckAndExpireBookingInternalAsync(booking, context);
    }

    public async Task<List<InvoiceResponseDto>> GetUserBookingsAsync(Guid userId)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var bookings = await context.Bookings
            .Include(b => b.Table)
            .Include(b => b.Timeslot)
            .Include(b => b.User)
            .Include(b => b.BookingDetails)
                .ThenInclude(d => d.MenuItem)
            .Where(b => b.UserId == userId)
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync();

        foreach (var b in bookings)
        {
            await CheckAndExpireBookingInternalAsync(b, context);
        }

        return bookings.Select(MapToInvoiceDto).ToList();
    }

    private async Task<bool> CheckAndExpireBookingInternalAsync(Booking booking, AppDbContext context)
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        if (booking.Status == "MenungguBayar" && now > booking.ExpiresAt)
        {
            booking.Status = "Kedaluwarsa";
            await context.SaveChangesAsync();
            _logger.LogInformation("Booking {BookingId} ({InvoiceCode}) marked as Kedaluwarsa.", booking.Id, booking.InvoiceCode);
            return true;
        }
        return false;
    }

    private static InvoiceResponseDto MapToInvoiceDto(Booking booking)
    {
        var endTime = booking.Timeslot.StartTime.AddHours(booking.DurationHours);

        var startTimeSpan = booking.Timeslot?.StartTime.ToTimeSpan() ?? TimeSpan.Zero;
        var endTimeSpan = startTimeSpan + TimeSpan.FromHours(booking.DurationHours);

        return new InvoiceResponseDto
        {
            BookingId = booking.Id,
            InvoiceCode = booking.InvoiceCode,
            TableNumber = booking.Table?.TableNumber ?? "-",
            Area = booking.Table?.Area ?? "-",
            Capacity = booking.Table?.Capacity ?? 0,
            RepresentativeName = booking.RepresentativeName,
            CustomerPhone = booking.User?.PhoneNumber ?? "-",
            BookingDate = booking.BookingDate,
            StartTime = startTimeSpan,
            EndTime = endTimeSpan,
            DurationHours = booking.DurationHours,
            Status = booking.Status,
            PaymentMethod = booking.PaymentMethod,
            PaymentProofUrl = booking.PaymentProofUrl,
            TotalAmount = booking.TotalAmount,
            ExpiresAt = booking.ExpiresAt,
            CreatedAt = booking.CreatedAt,
            Items = booking.BookingDetails.Select(d => new OrderItemDto
            {
                MenuItemId = d.MenuItemId,
                Name = d.MenuItem?.Name ?? "-",
                Quantity = d.Quantity,
                UnitPrice = d.UnitPrice
            }).ToList()
        };
    }
}
