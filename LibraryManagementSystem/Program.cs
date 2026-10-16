using LibraryManagementSystem.Data;
using LibraryManagementSystem.Models;
using LibraryManagementSystem.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// =========================================================
// DATABASE
// =========================================================

var connectionString = builder.Configuration
    .GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        connectionString,
        sqlOptions =>
        {
            sqlOptions.EnableRetryOnFailure(
                maxRetryCount: 3,
                maxRetryDelay: TimeSpan.FromSeconds(5),
                errorNumbersToAdd: null);

            sqlOptions.CommandTimeout(120);
        }));

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// =========================================================
// IDENTITY / AUTHENTICATION
// =========================================================

builder.Services
    .AddDefaultIdentity<IdentityUser>(options =>
        options.SignIn.RequireConfirmedAccount = false)
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

// =========================================================
// MVC
// =========================================================

builder.Services.AddControllersWithViews();

// =========================================================
// SQUARE CONFIGURATION
// =========================================================

builder.Services.Configure<SquareSettings>(
    builder.Configuration.GetSection("Square"));

// =========================================================
// SESSION
// =========================================================

// Used by the self-service kiosk to remember
// the verified borrower between screens.
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(10);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// =========================================================
// CORE LIBRARY SERVICES
// =========================================================

builder.Services.AddScoped<
    IFineCalculator,
    FineCalculator>();

builder.Services.AddScoped<
    IBorrowerEligibilityService,
    BorrowerEligibilityService>();

builder.Services.AddScoped<
    IAuditService,
    AuditService>();

// =========================================================
// SQUARE PAYMENT
// =========================================================

builder.Services.AddScoped<
    IPaymentService,
    SquarePaymentService>();

// =========================================================
// MULTI-BRANCH INVENTORY / TRANSFERS
// =========================================================

builder.Services.AddScoped<
    ITransferService,
    TransferService>();

// =========================================================
// NOTIFICATIONS
// =========================================================

builder.Services.AddScoped<
    INotificationService,
    NotificationService>();

// =========================================================
// RESERVATIONS / WAITLIST
// =========================================================

builder.Services.AddScoped<
    IReservationService,
    ReservationService>();

// =========================================================
// IMPORT / REPORTING
// =========================================================

builder.Services.AddScoped<
    IImportService,
    ImportService>();

builder.Services.AddScoped<
    IReportingService,
    ReportingService>();

// Scheduled monthly PDF / weekly reporting service.
builder.Services.AddHostedService<
    ScheduledReportService>();

// =========================================================
// API / SWAGGER
// =========================================================

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// =========================================================
// BACKGROUND OVERDUE PROCESSING
// =========================================================

builder.Services.AddHostedService<
    OverdueProcessingService>();

// =========================================================
// EMAIL
// =========================================================

builder.Services.AddTransient<
    IEmailSender,
    DevelopmentEmailSender>();

builder.Services.AddTransient<
    IEmailSender<IdentityUser>,
    DevelopmentEmailSender>();

var app = builder.Build();

// =========================================================
// DATABASE MIGRATIONS + SEEDING
// =========================================================

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;

    var context =
        services.GetRequiredService<ApplicationDbContext>();

    try
    {
        // Apply any migrations introduced by merged features.
        await context.Database.MigrateAsync();

        // Ensure roles and development users still exist.
        await RoleSeeder.SeedRolesAsync(services);
        await IdentitySeeder.SeedUsersAsync(services);

        // Seed core library data.
        await LibraryDataSeeder.SeedAsync(context);
    }
    catch (Exception ex)
    {
        app.Logger.LogError(
            ex,
            "Database initialization failed during application startup.");

        throw;
    }
}

// =========================================================
// HTTP REQUEST PIPELINE
// =========================================================

if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();

    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseRouting();

// Kiosk session must be available before controller actions.
app.UseSession();

// Identity authentication / authorization.
app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

// Attribute-routed API controllers.
app.MapControllers();

// Standard MVC routes.
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapRazorPages()
    .WithStaticAssets();

app.Run();