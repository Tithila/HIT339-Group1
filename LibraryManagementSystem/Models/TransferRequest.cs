
using System;
using System.ComponentModel.DataAnnotations;

namespace LibraryManagementSystem.Models
{
    public class TransferRequest
    {
        public int Id { get; set; }

        // The item being transferred.
        [Required]
        public int ItemId { get; set; }

        public Item? Item { get; set; }

        // The branch currently holding the item.
        [Required]
        public int FromBranchId { get; set; }

        public Branch? FromBranch { get; set; }

        // The branch receiving the item.
        [Required]
        public int ToBranchId { get; set; }

        public Branch? ToBranch { get; set; }

        // Identity user ID of the person requesting the transfer.
        [Required]
        [StringLength(450)]
        public string RequestedBy { get; set; } = string.Empty;

        public DateTime RequestedDate { get; set; } = DateTime.UtcNow;

        public TransferStatus Status { get; set; } =
            TransferStatus.Requested;

        public DateTime? CompletedDate { get; set; }
    }

    public enum TransferStatus
    {
        Requested = 0,
        Approved = 1,
        InTransit = 2,
        Completed = 3,
        Rejected = 4
    }
}
