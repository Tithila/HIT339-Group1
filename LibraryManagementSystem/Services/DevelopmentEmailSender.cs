using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;

namespace LibraryManagementSystem.Services;

public class DevelopmentEmailSender :
    IEmailSender,
    IEmailSender<IdentityUser>
{
    public Task SendEmailAsync(
        string email,
        string subject,
        string htmlMessage)
    {
        Console.WriteLine("====================================");
        Console.WriteLine($"EMAIL TO: {email}");
        Console.WriteLine($"SUBJECT: {subject}");
        Console.WriteLine($"MESSAGE: {htmlMessage}");
        Console.WriteLine("====================================");

        return Task.CompletedTask;
    }

    public Task SendConfirmationLinkAsync(
        IdentityUser user,
        string email,
        string confirmationLink)
    {
        return SendEmailAsync(
            email,
            "Confirm your email",
            $"Confirm your account here: {confirmationLink}");
    }

    public Task SendPasswordResetLinkAsync(
        IdentityUser user,
        string email,
        string resetLink)
    {
        return SendEmailAsync(
            email,
            "Reset your password",
            $"Reset your password here: {resetLink}");
    }

    public Task SendPasswordResetCodeAsync(
        IdentityUser user,
        string email,
        string resetCode)
    {
        return SendEmailAsync(
            email,
            "Password reset code",
            $"Your password reset code is: {resetCode}");
    }
}