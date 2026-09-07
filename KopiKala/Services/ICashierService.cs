using KopiKala.DTOs.Staff;

namespace KopiKala.Services;

public interface ICashierService
{
    Task<List<CashierTableStatusDto>> GetTableStatusesAsync(DateOnly date, int timeslotId);
    Task<List<CashierBookingDto>> GetPendingVerificationsAsync();
    Task<List<CashierBookingDto>> SearchBookingsAsync(string? query = null, DateOnly? date = null);
    Task<CashierBookingDto?> GetBookingDetailsAsync(Guid bookingId);
    Task<bool> VerifyPaymentAsync(Guid bookingId, bool approved, string? notes = null);
    Task<bool> CheckInGuestAsync(Guid bookingId);
    Task<Guid> CreateWalkInBookingAsync(WalkInBookingRequestDto dto, Guid cashierId);
    Task<bool> AddOrderToActiveBookingAsync(AddOnOrderRequestDto dto);
    Task<bool> SubstituteMenuItemAsync(SubstituteItemRequestDto dto);
    Task<bool> ExtendBookingDurationAsync(ExtendDurationRequestDto dto);
    Task<bool> CompleteBookingSessionAsync(Guid bookingId);
}
