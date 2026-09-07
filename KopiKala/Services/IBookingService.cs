using KopiKala.DTOs.Booking;

namespace KopiKala.Services;

public interface IBookingService
{
    Task<List<DiningTableDto>> GetAvailableTablesAsync(DateOnly date, int timeslotId);
    Task<List<TimeslotDto>> GetTimeslotsAsync();
    Task<List<MenuItemDto>> GetAvailableMenuItemsAsync();
    Task<Guid> CreateBookingAsync(CreateBookingRequestDto dto, Guid userId);
    Task<InvoiceResponseDto?> GetInvoiceAsync(Guid bookingId);
    Task<InvoiceResponseDto?> GetInvoiceByCodeAsync(string invoiceCode);
    Task<bool> UploadPaymentProofAsync(Guid bookingId, Stream fileStream, string fileName);
    Task<bool> ConfirmPayOnSiteAsync(Guid bookingId);
    Task<bool> CheckAndExpireBookingAsync(Guid bookingId);
    Task<List<InvoiceResponseDto>> GetUserBookingsAsync(Guid userId);
}
