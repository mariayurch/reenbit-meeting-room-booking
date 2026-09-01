using MeetingRoomBooking.Domain.Bookings;
using MeetingRoomBooking.Domain.MeetingRooms;
using MeetingRoomBooking.Domain.TimeSlots;
using Microsoft.EntityFrameworkCore;
using MeetingRoomBooking.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

namespace MeetingRoomBooking.Infrastructure.Persistence;

public sealed class ApplicationDbContext(
    DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>(options)
{
    public DbSet<MeetingRoom> MeetingRooms => Set<MeetingRoom>();

    public DbSet<TimeSlot> TimeSlots => Set<TimeSlot>();

    public DbSet<Booking> Bookings => Set<Booking>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<IdentityUserLogin<Guid>>(entity =>
        {
            entity.Property(login => login.LoginProvider)
                .HasMaxLength(128);

            entity.Property(login => login.ProviderKey)
                .HasMaxLength(128);
        });

        modelBuilder.Entity<IdentityUserToken<Guid>>(entity =>
        {
            entity.Property(token => token.LoginProvider)
                .HasMaxLength(128);

            entity.Property(token => token.Name)
                .HasMaxLength(128);
        });

        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(ApplicationDbContext).Assembly);
    }
}