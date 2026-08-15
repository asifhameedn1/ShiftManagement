using Domain.Common;
using FluentValidation;
using System;
using System.Collections.Generic;

namespace Application.Features.Security.Commands.UpdateEmployeeRoles;

public sealed record UpdateEmployeeRolesCommand(Guid EmployeeId, List<Guid> RoleIds);

public sealed class UpdateEmployeeRolesCommandValidator : AbstractValidator<UpdateEmployeeRolesCommand>
{
    public UpdateEmployeeRolesCommandValidator()
    {
        RuleFor(x => x.EmployeeId)
            .NotEmpty().WithMessage("Employee ID is required.");

        RuleFor(x => x.RoleIds)
            .NotNull().WithMessage("Role IDs list cannot be null.");
    }
}
