
using LibraryManagementSystem.Data;
using LibraryManagementSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagementSystem.Services;

public class ReservationService : IReservationService
{
    private readonly ApplicationDbContext _context;
    private readonly INotificationService _notificationService;

    public ReservationService(
        ApplicationDbContext context,
        INotificationService notificationService)
    {
        _context = context;
        _notificationService = notificationService;
    }

    public async Task<Reservation> PlaceReservationAsync(
        int itemId,
        int borrowerId)
    {
        var item = await _context.Items
            .FirstOrDefaultAsync(i => i.Id == itemId);

        if (item == null)
            throw new InvalidOperationException(
                "Library item not found.");

        if (item.Status != ItemStatus.Borrowed &&
            item.Status != ItemStatus.Damaged)
            throw new InvalidOperationException(
                "Reservations are only allowed for borrowed or damaged items.");

        var borrower = await _context.Borrowers
            .FirstOrDefaultAsync(b => b.Id == borrowerId);

        if (borrower == null)
            throw new InvalidOperationException(
                "Borrower not found.");

        if (borrower.IsSuspended)
            throw new InvalidOperationException(
                "Suspended borrowers cannot place reservations.");

        var alreadyReserved = await _context.Reservations
            .AnyAsync(r =>
                r.ItemId == itemId &&
                r.BorrowerId == borrowerId &&
                (r.Status == ReservationStatus.Waiting ||
                 r.Status == ReservationStatus.ReadyForPickup));

        if (alreadyReserved)
            throw new InvalidOperationException(
                "You already have an active reservation for this item.");

        var reservation = new Reservation
        {
            ItemId = itemId,
            BorrowerId = borrowerId,
            RequestedDate = DateTime.UtcNow,
            Status = ReservationStatus.Waiting
        };

        _context.Reservations.Add(reservation);
        await _context.SaveChangesAsync();

        return reservation;
    }

    public async Task<List<Reservation>> GetWaitlistAsync(int itemId)
    {
        return await _context.Reservations
            .Include(r => r.Borrower)
            .Include(r => r.Item)
            .Where(r =>
                r.ItemId == itemId &&
                r.Status == ReservationStatus.Waiting)
            .OrderBy(r => r.RequestedDate)
            .ThenBy(r => r.Id)
            .ToListAsync();
    }

    public async Task<bool> CancelReservationAsync(int reservationId)
    {
        var reservation = await _context.Reservations
            .FirstOrDefaultAsync(r => r.Id == reservationId);

        if (reservation == null)
            return false;

        if (reservation.Status != ReservationStatus.Waiting &&
            reservation.Status != ReservationStatus.ReadyForPickup)
            return false;

        reservation.Status = ReservationStatus.Cancelled;

        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<Reservation?> PromoteNextAsync(int itemId)
    {
        var item = await _context.Items
            .FirstOrDefaultAsync(i => i.Id == itemId);

        if (item == null || item.Status != ItemStatus.Available)
            return null;

        // Do not promote another patron if an item
        // already has an active pickup reservation.
        var alreadyReady = await _context.Reservations
            .AnyAsync(r =>
                r.ItemId == itemId &&
                r.Status == ReservationStatus.ReadyForPickup);

        if (alreadyReady)
            return null;

        var next = await _context.Reservations
            .Include(r => r.Borrower)
            .Where(r =>
                r.ItemId == itemId &&
                r.Status == ReservationStatus.Waiting)
            .OrderBy(r => r.RequestedDate)
            .ThenBy(r => r.Id)
            .FirstOrDefaultAsync();

        if (next == null)
            return null;

        next.Status = ReservationStatus.ReadyForPickup;
        next.ReadyDate = DateTime.UtcNow;
        next.ExpiryDate = DateTime.UtcNow.AddDays(3);

        await _context.SaveChangesAsync();

        if (next.Borrower != null)
        {
            await _notificationService.SendAsync(
                next.BorrowerId,
                next.Borrower.Email,
                NotificationChannel.Email,
                NotificationType.ReservationReady,
                $"Your reserved item '{item.Name}' is ready " +
                $"for pickup until {next.ExpiryDate:dd MMM yyyy}.");
        }

        return next;
    }
}
