using MeetingRoomBooking.Domain.Resources;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MeetingRoomBooking.Infrastructure.Persistence.Configurations;

public sealed class ResourceConfiguration
    : IEntityTypeConfiguration<Resource>
{
    public void Configure(EntityTypeBuilder<Resource> builder)
    {
        builder.ToTable("Resources");

        builder.HasKey(resource => resource.Id);

        builder.Property(resource => resource.Name)
            .HasMaxLength(100)
            .IsRequired();
    }
}