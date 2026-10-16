using LibraryManagementSystem.Data;
using LibraryManagementSystem.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Database connection
var connectionString = builder.Configuration
    .GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "Connection string 'DefaultConnection' not found.");

// SQL Server configuration with retry support
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

// Identity authentication and roles
builder.Services
    .AddDefaultIdentity<IdentityUser>(options =>
        options.SignIn.RequireConfirmedAccount = false)
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

// MVC
builder.Services.AddControllersWithViews();

// Existing library services
builder.Services.AddScoped<IFineCalculator, FineCalculator>();
builder.Services.AddScoped<
    IBorrowerEligibilityService,
    BorrowerEligibilityService>();
builder.Services.AddScoped<IAuditService, AuditService>();

// Anjana: Multi-Branch Transfer Service
builder.Services.AddScoped<ITransferService, TransferService>();

// Shereena: Automated Notifications
builder.Services.AddScoped<INotificationService, NotificationService>();

// Shereena: Digital Reservation and Waitlist
builder.Services.AddScoped<IReservationService, ReservationService>();

// Swagger / API
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Automated overdue processing
builder.Services.AddHostedService<OverdueProcessingService>();

// Email services
builder.Services.AddTransient<IEmailSender, DevelopmentEmailSender>();
builder.Services.AddTransient<
    IEmailSender<IdentityUser>,
    DevelopmentEmailSender>();

// Import and reporting services
builder.Services.AddScoped<IImportService, ImportService>();
builder.Services.AddScoped<IReportingService, ReportingService>();

// Scheduled report generation (monthly PDF + weekly digest)
builder.Services.AddHostedService<ScheduledReportService>();

var app = builder.Build();

// Apply database migrations and seed data
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.Migrate(); // applies pending migrations
    await LibraryDataSeeder.SeedAsync(db); // then run your seeder
}

// Configure the HTTP request pipeline
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

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();
app.MapControllers();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapRazorPages()
    .WithStaticAssets();

app.Run();
