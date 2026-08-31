using MeetingRoomBooking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using MeetingRoomBooking.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using MeetingRoomBooking.Application.MeetingRooms;
using MeetingRoomBooking.Infrastructure.MeetingRooms;
using MeetingRoomBooking.Application.TimeSlots;
using MeetingRoomBooking.Infrastructure.TimeSlots;
using MeetingRoomBooking.Application.Bookings;
using MeetingRoomBooking.Infrastructure.Bookings;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString(
    "DefaultConnection")
    ?? throw new InvalidOperationException(
        "Connection string 'DefaultConnection' was not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddScoped<IMeetingRoomQueries, MeetingRoomQueries>();
builder.Services.AddScoped<IMeetingRoomCommands, MeetingRoomCommands>();
builder.Services.AddScoped<ITimeSlotQueries, TimeSlotQueries>();
builder.Services.AddScoped<ITimeSlotCommands, TimeSlotCommands>();
builder.Services.AddSingleton<TimeProvider>(TimeProvider.System);
builder.Services.AddScoped<IBookingCommands, BookingCommands>();

builder.Services
    .AddDefaultIdentity<ApplicationUser>(options =>
    {
        options.SignIn.RequireConfirmedAccount = false;
    })
    .AddRoles<IdentityRole<Guid>>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.AddRazorPages();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider
        .GetRequiredService<RoleManager<IdentityRole<Guid>>>();

    var userManager = scope.ServiceProvider
        .GetRequiredService<UserManager<ApplicationUser>>();

    await IdentitySeeder.SeedRolesAsync(roleManager);

    var adminEmail = app.Configuration["SeedAdmin:Email"];
    var adminPassword = app.Configuration["SeedAdmin:Password"];
    var adminDisplayName = app.Configuration["SeedAdmin:DisplayName"];

    var hasAnyAdminSetting =
        !string.IsNullOrWhiteSpace(adminEmail)
        || !string.IsNullOrWhiteSpace(adminPassword)
        || !string.IsNullOrWhiteSpace(adminDisplayName);

    var hasAllAdminSettings =
        !string.IsNullOrWhiteSpace(adminEmail)
        && !string.IsNullOrWhiteSpace(adminPassword)
        && !string.IsNullOrWhiteSpace(adminDisplayName);

    if (hasAnyAdminSetting && !hasAllAdminSettings)
    {
        throw new InvalidOperationException(
            "Admin seed configuration is incomplete.");
    }

    if (hasAllAdminSettings)
    {
        await IdentitySeeder.SeedAdminAsync(
            userManager,
            adminEmail!,
            adminPassword!,
            adminDisplayName!);
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

app.Run();
