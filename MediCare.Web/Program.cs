using MediCare.Data;
using MediCare.Data.Entities;
using MediCare.Data.Repositories;
using MediCare.Services.Booking;
using MediCare.Services.MedicalRecords;
using MediCare.Web.DevTools;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MediCare.Services.Doctors;
using MediCare.Services.Specializations;
using MediCare.Services.Admin;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// ---------- SHARED setup (database + Identity) ----------
// If Member 1 already added Identity / DbContext in their branch, keep ONE copy of these lines.
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"))
    .ConfigureWarnings(warnings => warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning)));

builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    // Member 1 owns the real login page (/Account/Login).
    // In Development we use the demo sign-in page from DevTools until it exists.
    options.LoginPath = builder.Environment.IsDevelopment() ? "/dev" : "/Account/Login";
    options.AccessDeniedPath = builder.Environment.IsDevelopment() ? "/dev" : "/Account/AccessDenied";
});

builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// ---------- Member 2: Doctors ----------
builder.Services.AddDoctorServices();
builder.Services.AddSpecializationServices();


// ---------- Member 5: Medical Records & Prescriptions ----------
// Files are stored OUTSIDE wwwroot so they can only be downloaded through an authorized controller action.
builder.Services.AddMedicalRecordsModule(
    Path.Combine(builder.Environment.ContentRootPath, "App_Data", "medical-files"));

// ---------- Member 3: Appointment Booking ----------
builder.Services.AddAppointmentBookingModule();

// ---------- Admin Dashboard & Analytics ----------
builder.Services.AddAdminServices();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    // Seed admin account only (for now)
    await AdminSeeder.SeedAdminAsync(app.Services);

    // Demo data for testing the Medical Records module (delete DevTools when the team has real data).
    // await MedicalRecordsDevSeeder.SeedAsync(app.Services); // Temporarily disabled due to DB conflicts
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
