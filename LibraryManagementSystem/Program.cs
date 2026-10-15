
using LibraryManagementSystem.Data;
using LibraryManagementSystem.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.UI.Services;

var builder = WebApplication.CreateBuilder(args);

// Database connection
var connectionString = builder.Configuration
    .GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "Connection string 'DefaultConnection' not found.");

// Configure SQL Server with retry support and a longer timeout.
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

// Identity and role-based authentication
builder.Services
    .AddDefaultIdentity<IdentityUser>(options =>
        options.SignIn.RequireConfirmedAccount = false)
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

// MVC controllers and views
builder.Services.AddControllersWithViews();

// Existing application services
builder.Services.AddScoped<IFineCalculator, FineCalculator>();

builder.Services.AddScoped<
    IBorrowerEligibilityService,
    BorrowerEligibilityService>();

builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Multi-branch transfer service
builder.Services.AddScoped<ITransferService, TransferService>();

// Background service for overdue processing
builder.Services.AddHostedService<OverdueProcessingService>();

// Email services
builder.Services.AddTransient<
    IEmailSender,
    DevelopmentEmailSender>();

builder.Services.AddTransient<
    IEmailSender<IdentityUser>,
    DevelopmentEmailSender>();

var app = builder.Build();

// Apply migrations and seed initial data.
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;

    var context =
        services.GetRequiredService<ApplicationDbContext>();

    try
    {
        await context.Database.MigrateAsync();

        await RoleSeeder.SeedRolesAsync(services);

        await IdentitySeeder.SeedUsersAsync(services);

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

// Configure the HTTP request pipeline.
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
