
using System;
using System.ComponentModel.DataAnnotations;

namespace LibraryManagementSystem.Models;

public enum ReservationStatus
{
    Waiting,
    ReadyForPickup,
    Collected,
    Cancelled,
    Expired
}

public class Reservation
{
    public int Id { get; set; }

    [Required]
    public int ItemId { get; set; }

    public Item? Item { get; set; }

    [Required]
    public int BorrowerId { get; set; }

    public Borrower? Borrower { get; set; }

    public DateTime RequestedDate { get; set; }
        = DateTime.UtcNow;

    public ReservationStatus Status { get; set; }
        = ReservationStatus.Waiting;

    public DateTime? ReadyDate { get; set; }

    public DateTime? ExpiryDate { get; set; }

    public DateTime? CollectedDate { get; set; }
}
