
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using LibraryManagementSystem.Data;
using LibraryManagementSystem.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LibraryManagementSystem.Services;

public class ReservationService : IReservationService
{
    private readonly ApplicationDbContext _context;
    private readonly INotificationService _notificationService;
    private readonly ILogger<ReservationService> _logger;

    public ReservationService(
        ApplicationDbContext context,
        INotificationService notificationService,
        ILogger<ReservationService> logger)
    {
        _context = context;
        _notificationService = notificationService;
        _logger = logger;
    }

    // 1. Place a reservation
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

    // 2. Display the waiting list
    public async Task<List<Reservation>> GetWaitlistAsync(
        int itemId)
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

    // 3. Cancel an active reservation
    public async Task<bool> CancelReservationAsync(
        int reservationId)
    {
        var reservation = await _context.Reservations
            .FirstOrDefaultAsync(r => r.Id == reservationId);

        if (reservation == null)
            return false;

        if (reservation.Status != ReservationStatus.Waiting &&
            reservation.Status != ReservationStatus.ReadyForPickup)
            return false;

        bool wasReady =
            reservation.Status == ReservationStatus.ReadyForPickup;

        reservation.Status = ReservationStatus.Cancelled;
        await _context.SaveChangesAsync();

        if (wasReady)
        {
            try
            {
                await PromoteNextAsync(reservation.ItemId);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to promote next patron after cancellation for item {ItemId}",
                    reservation.ItemId);
            }
        }

        return true;
    }

    // 4. Promote the next waiting patron
    public async Task<Reservation?> PromoteNextAsync(
        int itemId)
    {
        var item = await _context.Items
            .FirstOrDefaultAsync(i => i.Id == itemId);

        if (item == null ||
            item.Status != ItemStatus.Available)
            return null;

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
                r.Status == ReservationStatus.Waiting &&
                r.Borrower != null &&
                !r.Borrower.IsSuspended)
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
            try
            {
                await _notificationService.SendAsync(
                    next.BorrowerId,
                    next.Borrower.Email,
                    NotificationChannel.Email,
                    NotificationType.ReservationReady,
                    $"Your reserved item '{item.Name}' " +
                    $"is ready for pickup until " +
                    $"{next.ExpiryDate:dd MMM yyyy}.");
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Pickup notification failed for reservation {ReservationId}",
                    next.Id);
            }
        }

        return next;
    }

    // 5. Collect a reserved item
    public async Task<Loan> CollectReservationAsync(
        int reservationId,
        DateTime dueDate)
    {
        if (dueDate.Date <= DateTime.Today)
        {
            throw new InvalidOperationException(
                "Due date must be in the future.");
        }

        var strategy =
            _context.Database.CreateExecutionStrategy();

        var result = await strategy.ExecuteAsync(async () =>
        {
            await using var transaction =
                await _context.Database.BeginTransactionAsync(
                    IsolationLevel.Serializable);

            var reservation = await _context.Reservations
                .Include(r => r.Item)
                .Include(r => r.Borrower)
                .FirstOrDefaultAsync(r => r.Id == reservationId);

            if (reservation == null)
            {
                throw new InvalidOperationException(
                    "Reservation not found.");
            }

            if (reservation.Status !=
                ReservationStatus.ReadyForPickup)
            {
                throw new InvalidOperationException(
                    "This reservation is not ready for collection.");
            }

            if (reservation.ExpiryDate.HasValue &&
                reservation.ExpiryDate.Value <= DateTime.UtcNow)
            {
                throw new InvalidOperationException(
                    "This reservation has expired.");
            }

            var item = reservation.Item;
            var borrower = reservation.Borrower;

            if (item == null || borrower == null)
            {
                throw new InvalidOperationException(
                    "Reservation item or borrower not found.");
            }

            if (item.Status != ItemStatus.Available)
            {
                throw new InvalidOperationException(
                    "The reserved item is not available.");
            }

            if (borrower.IsSuspended)
            {
                throw new InvalidOperationException(
                    "This borrower is currently suspended.");
            }

            var activeLoanExists = await _context.Loans
                .AnyAsync(l =>
                    l.ItemId == item.Id &&
                    l.ReturnDate == null);

            if (activeLoanExists)
            {
                throw new InvalidOperationException(
                    "This item already has an active loan.");
            }

            var loan = new Loan
            {
                ItemId = item.Id,
                BorrowerId = borrower.Id,
                BorrowDate = DateTime.Now,
                DueDate = dueDate,
                ReturnDate = null,
                IsOverdue = false,
                FineAmount = 0,
                FinePaid = false
            };

            _context.Loans.Add(loan);

            item.Status = ItemStatus.Borrowed;
            reservation.Status = ReservationStatus.Collected;
            reservation.CollectedDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return new
            {
                Loan = loan,
                BorrowerId = borrower.Id,
                BorrowerEmail = borrower.Email,
                BorrowerName = borrower.FullName,
                ItemName = item.Name,
                ReservationId = reservation.Id
            };
        });

        try
        {
            await _notificationService.SendAsync(
                result.BorrowerId,
                result.BorrowerEmail,
                NotificationChannel.Email,
                NotificationType.Borrowed,
                $"Hello {result.BorrowerName}, " +
                $"you collected your reserved item " +
                $"'{result.ItemName}'. Your due date is " +
                $"{dueDate:dd MMM yyyy}.");
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Collection notification failed for reservation {ReservationId}",
                result.ReservationId);
        }

        return result.Loan;
    }

    // 6. Automatically expire uncollected reservations
    public async Task<int> ProcessExpiredReservationsAsync()
    {
        var now = DateTime.UtcNow;

        var expiredReservations = await _context.Reservations
            .Where(r =>
                r.Status == ReservationStatus.ReadyForPickup &&
                r.ExpiryDate.HasValue &&
                r.ExpiryDate.Value <= now)
            .OrderBy(r => r.ExpiryDate)
            .ToListAsync();

        if (expiredReservations.Count == 0)
            return 0;

        // Update expired reservations before promoting
        // anyone else, so the old reservation no longer
        // blocks promotion.
        foreach (var reservation in expiredReservations)
        {
            reservation.Status = ReservationStatus.Expired;
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Expired {Count} uncollected reservations.",
            expiredReservations.Count);

        // Each affected item needs at most one promotion.
        var affectedItemIds = expiredReservations
            .Select(r => r.ItemId)
            .Distinct()
            .ToList();

        foreach (var itemId in affectedItemIds)
        {
            try
            {
                await PromoteNextAsync(itemId);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to promote next borrower for item {ItemId} after reservation expiry.",
                    itemId);
            }
        }

        return expiredReservations.Count;
    }
}
