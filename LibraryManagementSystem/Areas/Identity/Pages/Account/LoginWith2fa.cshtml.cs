using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace LibraryManagementSystem.Areas.Identity.Pages.Account;

public class LoginWith2faModel : PageModel
{
    private readonly SignInManager<IdentityUser> _signInManager;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly IEmailSender _emailSender;
    private readonly ILogger<LoginWith2faModel> _logger;

    public LoginWith2faModel(
        SignInManager<IdentityUser> signInManager,
        UserManager<IdentityUser> userManager,
        IEmailSender emailSender,
        ILogger<LoginWith2faModel> logger)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _emailSender = emailSender;
        _logger = logger;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public bool RememberMe { get; set; }

    public string? ReturnUrl { get; set; }

    public string? MaskedEmail { get; set; }

    public class InputModel
    {
        [Required]
        [StringLength(
            6,
            ErrorMessage = "The verification code must be 6 digits.",
            MinimumLength = 6)]
        [DataType(DataType.Text)]
        [Display(Name = "Email verification code")]
        public string TwoFactorCode { get; set; } = string.Empty;

        [Display(Name = "Remember this machine")]
        public bool RememberMachine { get; set; }
    }

    public async Task<IActionResult> OnGetAsync(
        bool rememberMe,
        string? returnUrl = null)
    {
        var user =
            await _signInManager.GetTwoFactorAuthenticationUserAsync();

        if (user == null)
        {
            throw new InvalidOperationException(
                "Unable to load two-factor authentication user.");
        }

        ReturnUrl = returnUrl;
        RememberMe = rememberMe;

        if (string.IsNullOrWhiteSpace(user.Email))
        {
            throw new InvalidOperationException(
                "The staff account does not have an email address.");
        }

        MaskedEmail = MaskEmail(user.Email);

        var code = await _userManager.GenerateTwoFactorTokenAsync(
            user,
            TokenOptions.DefaultEmailProvider);

        await _emailSender.SendEmailAsync(
            user.Email,
            "Library staff login verification code",
            $"Your two-factor authentication code is: {code}");

        _logger.LogInformation(
            "Email 2FA code generated for user {UserId}.",
            user.Id);

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(
        bool rememberMe,
        string? returnUrl = null)
    {
        returnUrl ??= Url.Content("~/");

        var user =
            await _signInManager.GetTwoFactorAuthenticationUserAsync();

        if (user == null)
        {
            throw new InvalidOperationException(
                "Unable to load two-factor authentication user.");
        }

        RememberMe = rememberMe;

        if (!string.IsNullOrWhiteSpace(user.Email))
        {
            MaskedEmail = MaskEmail(user.Email);
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var code = Input.TwoFactorCode
            .Replace(" ", string.Empty)
            .Replace("-", string.Empty);

        var result =
            await _signInManager.TwoFactorSignInAsync(
                TokenOptions.DefaultEmailProvider,
                code,
                rememberMe,
                Input.RememberMachine);

        if (result.Succeeded)
        {
            _logger.LogInformation(
                "User {UserId} logged in using email 2FA.",
                user.Id);

            return LocalRedirect(returnUrl);
        }

        if (result.IsLockedOut)
        {
            _logger.LogWarning(
                "User {UserId} account locked out.",
                user.Id);

            return RedirectToPage("./Lockout");
        }

        _logger.LogWarning(
            "Invalid email 2FA code entered for user {UserId}.",
            user.Id);

        ModelState.AddModelError(
            string.Empty,
            "Invalid verification code.");

        return Page();
    }

    private static string MaskEmail(string email)
    {
        var parts = email.Split('@');

        if (parts.Length != 2 || parts[0].Length <= 2)
        {
            return email;
        }

        var name = parts[0];

        return $"{name[0]}***{name[^1]}@{parts[1]}";
    }
}