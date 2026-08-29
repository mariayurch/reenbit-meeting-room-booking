using MeetingRoomBooking.Domain.Bookings;
using MeetingRoomBooking.Domain.MeetingRooms;
using MeetingRoomBooking.Domain.TimeSlots;
using MeetingRoomBooking.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MeetingRoomBooking.Infrastructure.Persistence.Configurations;

public sealed class BookingConfiguration
    : IEntityTypeConfiguration<Booking>
{
    public void Configure(EntityTypeBuilder<Booking> builder)
    {
        builder.ToTable("Bookings");

        builder.HasKey(booking => booking.Id);

        builder.Property(booking => booking.UserId)
            .IsRequired();

        builder.Property(booking => booking.MeetingRoomId)
            .IsRequired();

        builder.Property(booking => booking.StartUtc)
            .HasPrecision(0)
            .IsRequired();

        builder.Property(booking => booking.EndUtc)
            .HasPrecision(0)
            .IsRequired();

        builder.Property(booking => booking.CreatedAtUtc)
            .HasPrecision(0)
            .IsRequired();

        builder.Property(booking => booking.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(booking => booking.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<MeetingRoom>()
            .WithMany()
            .HasForeignKey(booking => booking.MeetingRoomId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(booking => booking.TimeSlots)
            .WithMany(slot => slot.Bookings)
            .UsingEntity<Dictionary<string, object>>(
                "BookingTimeSlots",
                join => join
                    .HasOne<TimeSlot>()
                    .WithMany()
                    .HasForeignKey("TimeSlotId")
                    .OnDelete(DeleteBehavior.Restrict),
                join => join
                    .HasOne<Booking>()
                    .WithMany()
                    .HasForeignKey("BookingId")
                    .OnDelete(DeleteBehavior.Cascade),
                join =>
                {
                    join.ToTable("BookingTimeSlots");

                    join.HasKey("BookingId", "TimeSlotId");

                    join.HasIndex("TimeSlotId")
                        .IsUnique()
                        .HasDatabaseName(
                            "UX_BookingTimeSlots_TimeSlotId");
                });

        builder.ToTable(table =>
            table.HasCheckConstraint(
                "CK_Bookings_EndUtc_After_StartUtc",
                "[EndUtc] > [StartUtc]"));
    }
}