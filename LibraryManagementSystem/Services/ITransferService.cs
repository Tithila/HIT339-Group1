
using LibraryManagementSystem.Models;

namespace LibraryManagementSystem.Services;

public interface ITransferService
{
    // Retrieve all transfer requests.
    Task<List<TransferRequest>> GetAllAsync();

    // Retrieve one transfer request.
    Task<TransferRequest?> GetByIdAsync(int id);

    // Create a new inter-branch transfer request.
    Task<TransferRequest> RequestTransferAsync(
        int itemId,
        int toBranchId,
        string requestedBy);

    // Approve a requested transfer.
    Task ApproveTransferAsync(
        int transferId,
        string userName);

    // Reject a requested transfer.
    Task RejectTransferAsync(
        int transferId,
        string userName);

    // Mark an approved transfer as in transit.
    Task MarkInTransitAsync(
        int transferId,
        string userName);

    // Complete a transfer and update the item's branch.
    Task CompleteTransferAsync(
        int transferId,
        string userName);
}
