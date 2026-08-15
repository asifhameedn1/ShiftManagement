using Domain.Common;
using FluentValidation;
using System;
using System.Collections.Generic;

namespace Application.Features.Security.Commands.UpdateRole;

public sealed record UpdateRoleCommand(Guid Id, string Name, List<Guid> PermissionIds);

public sealed class UpdateRoleCommandValidator : AbstractValidator<UpdateRoleCommand>
{
    public UpdateRoleCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Role ID is required.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Role name is required.")
            .MaximumLength(50).WithMessage("Role name must not exceed 50 characters.");
    }
}
