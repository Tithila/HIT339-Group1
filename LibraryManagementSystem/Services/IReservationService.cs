
using LibraryManagementSystem.Models;

namespace LibraryManagementSystem.Services;

public interface IReservationService
{
    // Create a reservation
    Task<Reservation> PlaceReservationAsync(
        int itemId,
        int borrowerId);

    // View the waiting list
    Task<List<Reservation>> GetWaitlistAsync(
        int itemId);

    // Cancel a reservation
    Task<bool> CancelReservationAsync(
        int reservationId);

    // Promote the next waiting borrower
    Task<Reservation?> PromoteNextAsync(
        int itemId);

    // Collect a reserved library item
    Task<Loan> CollectReservationAsync(
        int reservationId,
        DateTime dueDate);

    // Automatically expire uncollected reservations
    Task<int> ProcessExpiredReservationsAsync();
}
