using Domain.Common;
using FluentValidation;
using System;
using System.Collections.Generic;

namespace Application.Features.Security.Commands.UpdateEmployeeDepartmentRoles;

/// <summary>A single role together with every department it should be granted in.</summary>
public sealed record RoleDepartmentsAssignment(Guid RoleId, List<Guid> DepartmentIds);

/// <summary>
/// Replaces the employee's full set of (role, department) assignments. Each role carries its own
/// list of departments, so different roles can apply to different departments.
/// </summary>
public sealed record UpdateEmployeeDepartmentRolesCommand(Guid EmployeeId, List<RoleDepartmentsAssignment> Assignments);

public sealed class UpdateEmployeeDepartmentRolesCommandValidator : AbstractValidator<UpdateEmployeeDepartmentRolesCommand>
{
    public UpdateEmployeeDepartmentRolesCommandValidator()
    {
        RuleFor(x => x.EmployeeId)
            .NotEmpty().WithMessage("Employee ID is required.");

        RuleFor(x => x.Assignments)
            .NotNull().WithMessage("Assignments list cannot be null.");
    }
}
