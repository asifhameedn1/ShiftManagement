using Domain.Common;
using FluentValidation;
using System;
using System.Collections.Generic;

namespace Application.Features.Security.Commands.CreateRole;

public sealed record CreateRoleCommand(string Name, List<Guid> PermissionIds);

public sealed class CreateRoleCommandValidator : AbstractValidator<CreateRoleCommand>
{
    public CreateRoleCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Role name is required.")
            .MaximumLength(50).WithMessage("Role name must not exceed 50 characters.");
    }
}
