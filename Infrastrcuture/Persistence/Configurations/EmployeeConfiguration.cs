using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public sealed class EmployeeConfiguration : IEntityTypeConfiguration<Employee>
{
    public void Configure(EntityTypeBuilder<Employee> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(e => e.Username)
            .IsRequired()
            .HasMaxLength(100);

        // Unique index so the same AD account can't be registered twice
        builder.HasIndex(e => e.Username)
            .IsUnique();

        builder.Property(e => e.IsActive)
            .IsRequired();

        builder.Property(e => e.CreatedAt)
            .IsRequired();

        // Navigation — one employee has many shifts
        builder.HasMany(e => e.Shifts)
            .WithOne(s => s.Employee)
            .HasForeignKey(s => s.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(e => e.Departments)
            .WithMany(d => d.Employees)
            .UsingEntity("EmployeeDepartments");
    }
}
