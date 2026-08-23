using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public sealed class EmployeeDepartmentRoleConfiguration : IEntityTypeConfiguration<EmployeeDepartmentRole>
{
    public void Configure(EntityTypeBuilder<EmployeeDepartmentRole> builder)
    {
        builder.HasKey(x => x.Id);

        builder.HasIndex(x => new { x.EmployeeId, x.DepartmentId, x.RoleId })
            .IsUnique();

        builder.HasOne(x => x.Employee)
            .WithMany(e => e.DepartmentRoles)
            .HasForeignKey(x => x.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Department)
            .WithMany()
            .HasForeignKey(x => x.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Role)
            .WithMany()
            .HasForeignKey(x => x.RoleId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
