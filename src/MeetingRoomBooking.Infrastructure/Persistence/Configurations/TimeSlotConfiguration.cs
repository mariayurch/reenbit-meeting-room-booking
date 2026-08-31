using MeetingRoomBooking.Domain.TimeSlots;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MeetingRoomBooking.Infrastructure.Persistence.Configurations;

public sealed class TimeSlotConfiguration
    : IEntityTypeConfiguration<TimeSlot>
{
    public void Configure(EntityTypeBuilder<TimeSlot> builder)
    {
        builder.ToTable("TimeSlots");

        builder.HasKey(slot => slot.Id);

        builder.Property(slot => slot.StartUtc)
            .HasPrecision(0)
            .IsRequired();

        builder.Property(slot => slot.EndUtc)
            .HasPrecision(0)
            .IsRequired();

        builder.HasIndex(slot => new
        {
            slot.MeetingRoomId,
            slot.StartUtc
        })
            .IsUnique();

        builder.ToTable(table =>
            table.HasCheckConstraint(
                "CK_TimeSlots_EndUtc_After_StartUtc",
                "[EndUtc] > [StartUtc]"));
    }
}