using LibraryManagementSystem.Data;
using LibraryManagementSystem.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.UI.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services
    .AddDefaultIdentity<IdentityUser>(options =>
        options.SignIn.RequireConfirmedAccount = false)
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddControllersWithViews();

// Session is used by the self-service kiosk
// to remember the verified borrower between screens.
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(10);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddScoped<IFineCalculator, FineCalculator>();
builder.Services.AddScoped<IBorrowerEligibilityService, BorrowerEligibilityService>();
builder.Services.AddScoped<IAuditService, AuditService>();

// Swagger / OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddHostedService<OverdueProcessingService>();

builder.Services.AddTransient<IEmailSender, DevelopmentEmailSender>();
builder.Services.AddTransient<IEmailSender<IdentityUser>, DevelopmentEmailSender>();

var app = builder.Build();

// Create roles and seed library data automatically
// when the application starts.
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;

    await RoleSeeder.SeedRolesAsync(services);
    await IdentitySeeder.SeedUsersAsync(services);

    var context =
        services.GetRequiredService<ApplicationDbContext>();

    await LibraryDataSeeder.SeedAsync(context);
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

// Session must be enabled after routing
// and before controller actions use HttpContext.Session.
app.UseSession();

app.UseAuthorization();

app.MapStaticAssets();

// Attribute-routed API controllers
app.MapControllers();

// Standard MVC routes
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapRazorPages()
    .WithStaticAssets();

app.Run();