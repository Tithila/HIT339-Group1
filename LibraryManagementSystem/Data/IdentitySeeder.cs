using Microsoft.AspNetCore.Identity;

namespace LibraryManagementSystem.Data;

public static class IdentitySeeder
{
    public static async Task SeedUsersAsync(IServiceProvider serviceProvider)
    {
        var userManager =
            serviceProvider.GetRequiredService<UserManager<IdentityUser>>();

        await CreateUserAsync(
            userManager,
            "admin@library.com",
            "Admin123!",
            "Admin");

        await CreateUserAsync(
            userManager,
            "reception@library.com",
            "Reception123!",
            "Reception");

        await CreateUserAsync(
            userManager,
            "manager@library.com",
            "Manager123!",
            "Manager");
    }

    private static async Task CreateUserAsync(
        UserManager<IdentityUser> userManager,
        string email,
        string password,
        string role)
    {
        var user = await userManager.FindByEmailAsync(email);

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
                return;
            }
        }

        if (!await userManager.IsInRoleAsync(user, role))
        {
            await userManager.AddToRoleAsync(user, role);
        }

        if (!await userManager.GetTwoFactorEnabledAsync(user))
        {
            await userManager.SetTwoFactorEnabledAsync(user, true);
        }
    }
}