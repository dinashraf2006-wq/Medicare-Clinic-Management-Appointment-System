using System.Net;
using System.Threading.RateLimiting;
using MediCare.Data;
using MediCare.Data.Entities;
using MediCare.Data.Repositories;
using MediCare.Services.Admin;
using MediCare.Services.Booking;
using MediCare.Services.Doctors;
using MediCare.Services.MedicalRecords;
using MediCare.Services.Specializations;
using MediCare.Web.Accounts;
using MediCare.Web.Demo;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
var isDemo = builder.Environment.IsEnvironment("Demo");
var useLocalSqlite = builder.Environment.IsDevelopment() && builder.Configuration.GetValue<bool>("UseLocalSqlite");
var allowDemoLoopbackHttp = isDemo && builder.Configuration.GetValue<bool>("Demo:AllowHttpOnLoopback");
var appDataPath = Path.Combine(builder.Environment.ContentRootPath, "App_Data");
var demoDataPath = Path.GetFullPath(builder.Configuration["Demo:DataDirectory"] ?? Path.Combine(appDataPath, "demo"));

builder.Services.AddControllersWithViews();
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    if (isDemo || useLocalSqlite)
    {
        var path = isDemo ? Path.Combine(demoDataPath, "demo-v2.sqlite3") : Path.Combine(appDataPath, "medicare-local.sqlite3");
        options.UseSqlite(new SqliteConnectionStringBuilder { DataSource = path }.ToString());
    }
    else
    {
        options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"))
            .ConfigureWarnings(warnings => warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
    }
});

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.User.RequireUniqueEmail = true;
    options.Password.RequiredLength = 10;
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
})
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
    if (isDemo)
    {
        options.Cookie.Name = ".MediCare.Demo";
        options.Cookie.SecurePolicy = allowDemoLoopbackHttp ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = false;
    }
});

if (isDemo)
{
    Directory.CreateDirectory(demoDataPath);
    builder.Services.AddDataProtection()
        .SetApplicationName("MediCare.PublicDemo")
        .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(demoDataPath, "keys")));
}

builder.Services.Configure<ForwardedHeadersOptions>(options =>
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto);
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("account", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 20, Window = TimeSpan.FromMinutes(10) }));
});

builder.Services.AddScoped<AccountProvisioner>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddDoctorServices();
builder.Services.AddSpecializationServices();
builder.Services.AddMedicalRecordsModule(Path.Combine(isDemo ? demoDataPath : appDataPath, "medical-files"));
builder.Services.AddAppointmentBookingModule(options =>
{
    options.ClinicTimeZone = TimeZoneInfo.FindSystemTimeZoneById(builder.Configuration["Booking:TimeZone"] ?? "Africa/Cairo");
    options.ConfirmAutomatically = isDemo;
});
builder.Services.AddAdminServices();

var app = builder.Build();

if (isDemo)
{
    await DemoData.InitializeAsync(app.Services);
}
else if (useLocalSqlite)
{
    Directory.CreateDirectory(appDataPath);
    await using var scope = app.Services.CreateAsyncScope();
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await LocalSqliteUpgrade.InitializeAsync(db);
}

if (!isDemo) await AdminBootstrap.InitializeAsync(app.Services, app.Configuration);

app.UseForwardedHeaders();
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

if (!allowDemoLoopbackHttp) app.UseHttpsRedirection();
if (isDemo)
{
    app.Use(async (context, next) =>
    {
        var localHost = context.Request.Host.Host is "localhost" or "127.0.0.1" or "[::1]";
        var localPreview = allowDemoLoopbackHttp && localHost &&
            context.Connection.RemoteIpAddress is { } address && IPAddress.IsLoopback(address);
        if (!context.Request.IsHttps && !localPreview)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsync("This demo requires HTTPS.");
            return;
        }

        context.Response.Headers["X-Robots-Tag"] = "noindex, nofollow";
        await next();
    });
}

app.UseRouting();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
