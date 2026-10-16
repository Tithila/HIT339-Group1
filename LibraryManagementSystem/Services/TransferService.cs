
using LibraryManagementSystem.Data;
using LibraryManagementSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagementSystem.Services;

public class TransferService : ITransferService
{
    private readonly ApplicationDbContext _context;

    public TransferService(ApplicationDbContext context)
    {
        _context = context;
    }

    // Get all transfer requests, newest first.
    public async Task<List<TransferRequest>> GetAllAsync()
    {
        return await _context.TransferRequests
            .AsNoTracking()
            .Include(t => t.Item)
            .Include(t => t.FromBranch)
            .Include(t => t.ToBranch)
            .OrderByDescending(t => t.RequestedDate)
            .ToListAsync();
    }

    // Get a single transfer request.
    public async Task<TransferRequest?> GetByIdAsync(int id)
    {
        return await _context.TransferRequests
            .AsNoTracking()
            .Include(t => t.Item)
            .Include(t => t.FromBranch)
            .Include(t => t.ToBranch)
            .FirstOrDefaultAsync(t => t.Id == id);
    }

    // Request an item transfer.
    public async Task<TransferRequest> RequestTransferAsync(
        int itemId,
        int toBranchId,
        string requestedBy)
    {
        if (string.IsNullOrWhiteSpace(requestedBy))
            throw new ArgumentException(
                "A valid requester is required.");

        var item = await _context.Items
            .FirstOrDefaultAsync(i => i.Id == itemId);

        if (item == null)
            throw new InvalidOperationException(
                "The selected item does not exist.");

        if (!item.BranchId.HasValue)
            throw new InvalidOperationException(
                "Assign the item to a branch before transferring it.");

        if (item.BranchId.Value == toBranchId)
            throw new InvalidOperationException(
                "The destination must be different from the current branch.");

        if (item.Status != ItemStatus.Available)
            throw new InvalidOperationException(
                "Only available items can be transferred.");

        bool destinationExists = await _context.Branches
            .AnyAsync(b => b.Id == toBranchId);

        if (!destinationExists)
            throw new InvalidOperationException(
                "The destination branch does not exist.");

        bool activeTransferExists = await _context.TransferRequests
            .AnyAsync(t =>
                t.ItemId == itemId &&
                (t.Status == TransferStatus.Requested ||
                 t.Status == TransferStatus.Approved ||
                 t.Status == TransferStatus.InTransit));

        if (activeTransferExists)
            throw new InvalidOperationException(
                "This item already has an active transfer request.");

        var transfer = new TransferRequest
        {
            ItemId = item.Id,
            FromBranchId = item.BranchId.Value,
            ToBranchId = toBranchId,
            RequestedBy = requestedBy,
            RequestedDate = DateTime.UtcNow,
            Status = TransferStatus.Requested
        };

        _context.TransferRequests.Add(transfer);

        await SaveWithAuditAsync(
            transfer,
            "Transfer Requested",
            requestedBy,
            $"Item {item.LibraryCode} requested from branch " +
            $"{transfer.FromBranchId} to branch {toBranchId}.");

        return transfer;
    }

    // Approve a pending transfer.
    public async Task ApproveTransferAsync(
        int transferId,
        string userName)
    {
        var transfer = await FindTransferAsync(transferId);

        if (transfer.Status != TransferStatus.Requested)
            throw new InvalidOperationException(
                "Only requested transfers can be approved.");

        await EnsureItemStillAtSourceAsync(transfer);

        transfer.Status = TransferStatus.Approved;

        await SaveWithAuditAsync(
            transfer,
            "Transfer Approved",
            userName,
            $"Transfer {transfer.Id} was approved.");
    }

    // Reject a pending transfer.
    public async Task RejectTransferAsync(
        int transferId,
        string userName)
    {
        var transfer = await FindTransferAsync(transferId);

        if (transfer.Status != TransferStatus.Requested &&
            transfer.Status != TransferStatus.Approved)
            throw new InvalidOperationException(
                "Only requested or approved transfers can be rejected.");

        transfer.Status = TransferStatus.Rejected;

        await SaveWithAuditAsync(
            transfer,
            "Transfer Rejected",
            userName,
            $"Transfer {transfer.Id} was rejected.");
    }

    // Mark an approved transfer as in transit.
    public async Task MarkInTransitAsync(
        int transferId,
        string userName)
    {
        var transfer = await FindTransferAsync(transferId);

        if (transfer.Status != TransferStatus.Approved)
            throw new InvalidOperationException(
                "Only approved transfers can be marked as in transit.");

        await EnsureItemStillAtSourceAsync(transfer);

        transfer.Status = TransferStatus.InTransit;

        await SaveWithAuditAsync(
            transfer,
            "Transfer In Transit",
            userName,
            $"Transfer {transfer.Id} is now in transit.");
    }

    // Complete the transfer and update the item's branch.
    public async Task CompleteTransferAsync(
        int transferId,
        string userName)
    {
        var transfer = await FindTransferAsync(transferId);

        if (transfer.Status != TransferStatus.InTransit)
            throw new InvalidOperationException(
                "Only in-transit transfers can be completed.");

        var item = await _context.Items
            .FirstOrDefaultAsync(i => i.Id == transfer.ItemId);

        if (item == null)
            throw new InvalidOperationException(
                "The transferred item no longer exists.");

        if (item.BranchId != transfer.FromBranchId)
            throw new InvalidOperationException(
                "The item's current branch does not match the transfer source.");

        if (item.Status != ItemStatus.Available)
            throw new InvalidOperationException(
                "The item is no longer available for transfer.");

        // Change the item's branch only when the transfer is completed.
        item.BranchId = transfer.ToBranchId;

        transfer.Status = TransferStatus.Completed;
        transfer.CompletedDate = DateTime.UtcNow;

        await SaveWithAuditAsync(
            transfer,
            "Transfer Completed",
            userName,
            $"Item {item.LibraryCode} moved from branch " +
            $"{transfer.FromBranchId} to branch {transfer.ToBranchId}.");
    }

    // Find a transfer for modification.
    private async Task<TransferRequest> FindTransferAsync(int id)
    {
        return await _context.TransferRequests
            .FirstOrDefaultAsync(t => t.Id == id)
            ?? throw new InvalidOperationException(
                "The transfer request does not exist.");
    }

    // Ensure the item has not moved or become unavailable.
    private async Task EnsureItemStillAtSourceAsync(
        TransferRequest transfer)
    {
        var item = await _context.Items
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.Id == transfer.ItemId);

        if (item == null ||
            item.BranchId != transfer.FromBranchId)
            throw new InvalidOperationException(
                "The item is no longer at the source branch.");

        if (item.Status != ItemStatus.Available)
            throw new InvalidOperationException(
                "The item is not available for transfer.");
    }

    // Save transfer changes and audit log in one transaction.
    // The execution strategy supports SQL Server retry settings.
    private async Task SaveWithAuditAsync(
        TransferRequest transfer,
        string action,
        string userName,
        string details)
    {
        if (string.IsNullOrWhiteSpace(userName))
            throw new ArgumentException(
                "A valid user is required.");

        var strategy = _context.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            // Save the transfer first to obtain its database ID.
            await _context.SaveChangesAsync();

            var auditLog = new AuditLog
            {
                Action = action,
                EntityName = nameof(TransferRequest),
                EntityId = transfer.Id,
                Details = details,
                UserName = userName,
                Timestamp = DateTime.UtcNow
            };

            _context.AuditLogs.Add(auditLog);

            await _context.SaveChangesAsync();

            await transaction.CommitAsync();
        });
    }
}
