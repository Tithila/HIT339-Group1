
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Hosting;

namespace LibraryManagementSystem.Data;

public static class IdentitySeeder
{
    public static async Task SeedUsersAsync(
        IServiceProvider serviceProvider)
    {
        var userManager =
            serviceProvider.GetRequiredService<UserManager<IdentityUser>>();

        var environment =
            serviceProvider.GetRequiredService<IHostEnvironment>();

        bool isDevelopment = environment.IsDevelopment();

        await CreateUserAsync(
            userManager,
            "admin@library.com",
            "Admin123!",
            "Admin",
            isDevelopment);

        await CreateUserAsync(
            userManager,
            "reception@library.com",
            "Reception123!",
            "Reception",
            isDevelopment);

        await CreateUserAsync(
            userManager,
            "manager@library.com",
            "Manager123!",
            "Manager",
            isDevelopment);
    }

    private static async Task CreateUserAsync(
        UserManager<IdentityUser> userManager,
        string email,
        string password,
        string role,
        bool isDevelopment)
    {
        var user = await userManager.FindByEmailAsync(email);

        // Create the account if it does not exist.
        if (user == null)
        {
            user = new IdentityUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true
            };

            var result =
                await userManager.CreateAsync(user, password);

            if (!result.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Failed to create user {email}: " +
                    string.Join("; ",
                        result.Errors.Select(e => e.Description)));
            }
        }

        // Ensure the account has the correct role.
        if (!await userManager.IsInRoleAsync(user, role))
        {
            var roleResult =
                await userManager.AddToRoleAsync(user, role);

            if (!roleResult.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Failed to assign role {role} to {email}.");
            }
        }

        // LOCAL DEVELOPMENT ONLY:
        // Unlock the Admin account.
        if (isDevelopment && role == "Admin")
        {
            var unlockResult =
                await userManager.SetLockoutEndDateAsync(
                    user, null);

            if (!unlockResult.Succeeded)
            {
                throw new InvalidOperationException(
                    "Failed to unlock the Admin account.");
            }

            var resetResult =
                await userManager.ResetAccessFailedCountAsync(user);

            if (!resetResult.Succeeded)
            {
                throw new InvalidOperationException(
                    "Failed to reset Admin failed login attempts.");
            }
        }

        // LOCAL DEVELOPMENT ONLY:
        // Disable 2FA for seeded test accounts.
        if (isDevelopment &&
            await userManager.GetTwoFactorEnabledAsync(user))
        {
            var result =
                await userManager.SetTwoFactorEnabledAsync(
                    user, false);

            if (!result.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Failed to disable development 2FA for {email}.");
            }
        }
    }
}
