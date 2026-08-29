using MeetingRoomBooking.Domain.MeetingRooms;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MeetingRoomBooking.Infrastructure.Persistence.Configurations;

public sealed class MeetingRoomConfiguration
    : IEntityTypeConfiguration<MeetingRoom>
{
    public void Configure(EntityTypeBuilder<MeetingRoom> builder)
    {
        builder.ToTable("MeetingRooms");

        builder.HasKey(room => room.Id);

        builder.Property(room => room.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(room => room.Description)
            .HasMaxLength(500);

        builder.Property(room => room.Capacity)
            .IsRequired();

        builder.Property(room => room.IsActive)
            .HasDefaultValue(true)
            .IsRequired();

        builder.HasIndex(room => room.Name)
            .IsUnique();

        builder.HasMany(room => room.TimeSlots)
            .WithOne(slot => slot.MeetingRoom)
            .HasForeignKey(slot => slot.MeetingRoomId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.ToTable(table =>
            table.HasCheckConstraint(
                "CK_MeetingRooms_Capacity_Positive",
                "[Capacity] > 0"));
    }
}