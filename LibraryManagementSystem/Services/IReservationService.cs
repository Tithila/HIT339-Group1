
using LibraryManagementSystem.Models;

namespace LibraryManagementSystem.Services;

public interface IReservationService
{
    Task<Reservation> PlaceReservationAsync(
        int itemId,
        int borrowerId);

    Task<List<Reservation>> GetWaitlistAsync(int itemId);

    Task<bool> CancelReservationAsync(int reservationId);

    Task<Reservation?> PromoteNextAsync(int itemId);
}
