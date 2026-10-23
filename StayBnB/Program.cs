using System.Data;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using StayBnB.Data;
using StayBnB.Models.Entities;
using StayBnB.Services;

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------------------------
// Database provider
// ---------------------------------------------------------------------------
// The project targets SQL Server (database.sql / StaybnbDb). When no SQL Server
// instance can be reached the application falls back to the EF Core in-memory
// provider so the app still starts and every screen stays usable.
// Set "Database:Provider" to "SqlServer" or "InMemory" to force one of them.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                       ?? "Server=(localdb)\\mssqllocaldb;Database=StaybnbDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True";

var providerPreference = builder.Configuration["Database:Provider"] ?? "Auto";
var useSqlServer = providerPreference switch
{
    "SqlServer" => true,
    "InMemory" => false,
    _ => CanReachSqlServer(connectionString)
};

if (useSqlServer)
{
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlServer(connectionString, sql =>
            sql.EnableRetryOnFailure(maxRetryCount: 3, maxRetryDelay: TimeSpan.FromSeconds(5), null)));
}
else
{
    // The in-memory fallback is only used when no SQL Server instance can be
    // reached. A fixed database name keeps every request in one process talking
    // to the same store instead of an isolated per-request copy, and the database
    // root is set explicitly so data survives an EF Core service-provider rebuild.
    var inMemoryRoot = new InMemoryDatabaseRoot();

    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseInMemoryDatabase("StayBnB", inMemoryRoot));
}

// ---------------------------------------------------------------------------
// Identity + MVC
// ---------------------------------------------------------------------------
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.Password.RequiredLength = 8;
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.SignIn.RequireConfirmedAccount = false;
        options.User.RequireUniqueEmail = true;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.Name = "StayBnB.Auth";
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;
});

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 25 * 1024 * 1024; // 25 MB of uploads per request
});

builder.Services.AddScoped<IActivityLogger, ActivityLogger>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IPricingService, PricingService>();
builder.Services.AddScoped<IFileStorageService, FileStorageService>();
builder.Services.AddScoped<IBookingService, BookingService>();
builder.Services.AddScoped<IHostApplicationService, HostApplicationService>();
builder.Services.AddScoped<IMessagingService, MessagingService>();
builder.Services.AddScoped<IReviewService, ReviewService>();

var app = builder.Build();

// ---------------------------------------------------------------------------
// Pipeline
// ---------------------------------------------------------------------------
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseStatusCodePagesWithReExecute("/Home/StatusCode", "?code={0}");
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// ---------------------------------------------------------------------------
// Database + seed
// ---------------------------------------------------------------------------
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    var db = services.GetRequiredService<ApplicationDbContext>();

    try
    {
        if (db.Database.IsRelational())
        {
            logger.LogInformation("Applying EF Core migrations to the SQL Server database.");
            db.Database.Migrate();
        }
        else
        {
            logger.LogWarning(
                "No SQL Server instance was reachable, so the in-memory provider is being used. " +
                "Set ConnectionStrings:DefaultConnection (and Database:Provider=SqlServer) to use the StaybnbDb SQL Server schema.");
            db.Database.EnsureCreated();
        }
        await RoleSeeder.SeedAsync(services, app.Configuration);
        await DemoDataSeeder.SeedAsync(services, app.Configuration);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Database initialisation failed.");
        throw;
    }
}

app.Run();

static bool CanReachSqlServer(string connectionString)
{
    try
    {
        var builder = new SqlConnectionStringBuilder(connectionString)
        {
            ConnectTimeout = 3,
            InitialCatalog = "master"
        };

        using var connection = new SqlConnection(builder.ConnectionString);
        connection.Open();
        return true;
    }
    catch (Exception)
    {
        return false;
    }
}

/// <summary>Exposed so the EF Core design-time tools can find the context.</summary>
public partial class Program { }
