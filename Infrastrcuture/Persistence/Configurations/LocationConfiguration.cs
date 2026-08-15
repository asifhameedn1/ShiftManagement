using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public sealed class LocationConfiguration : IEntityTypeConfiguration<Location>
{
    public void Configure(EntityTypeBuilder<Location> builder)
    {
        builder.HasKey(l => l.Id);

        builder.Property(l => l.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.HasIndex(l => new { l.DepartmentId, l.Name }).IsUnique();

        // Employees FK is configured here (Location owns many Employees)
        builder.HasMany(l => l.Employees)
            .WithOne(e => e.Location)
            .HasForeignKey(e => e.LocationId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
