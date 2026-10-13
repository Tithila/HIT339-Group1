namespace LibraryManagementSystem.Services;

public interface IBorrowerEligibilityService
{
    Task CheckAndUpdateSuspensionAsync(int borrowerId);
}