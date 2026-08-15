using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public sealed class DepartmentConfiguration : IEntityTypeConfiguration<Department>
{
    public void Configure(EntityTypeBuilder<Department> builder)
    {
        builder.HasKey(d => d.Id);

        builder.Property(d => d.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.HasIndex(d => d.Name).IsUnique();

        builder.HasMany(d => d.Locations)
            .WithOne(l => l.Department)
            .HasForeignKey(l => l.DepartmentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
