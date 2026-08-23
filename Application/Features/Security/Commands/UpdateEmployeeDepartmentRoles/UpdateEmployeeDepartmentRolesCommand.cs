using Domain.Common;
using FluentValidation;
using System;
using System.Collections.Generic;

namespace Application.Features.Security.Commands.UpdateEmployeeDepartmentRoles;

public sealed record UpdateEmployeeDepartmentRolesCommand(Guid EmployeeId, Guid DepartmentId, List<Guid> RoleIds);

public sealed class UpdateEmployeeDepartmentRolesCommandValidator : AbstractValidator<UpdateEmployeeDepartmentRolesCommand>
{
    public UpdateEmployeeDepartmentRolesCommandValidator()
    {
        RuleFor(x => x.EmployeeId)
            .NotEmpty().WithMessage("Employee ID is required.");

        RuleFor(x => x.DepartmentId)
            .NotEmpty().WithMessage("Department ID is required.");

        RuleFor(x => x.RoleIds)
            .NotNull().WithMessage("Role IDs list cannot be null.");
    }
}
