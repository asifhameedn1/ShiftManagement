using Domain.Common;
using FluentValidation;

namespace Application.Features.Employees.Commands.UpdateEmployeeLocation;

/// <summary>
/// Command to update the assigned location of an employee.
/// </summary>
public sealed record UpdateEmployeeLocationCommand(Guid EmployeeId, Guid? LocationId);

public sealed class UpdateEmployeeLocationCommandValidator : AbstractValidator<UpdateEmployeeLocationCommand>
{
    public UpdateEmployeeLocationCommandValidator()
    {
        RuleFor(x => x.EmployeeId)
            .NotEmpty()
            .WithMessage("Employee ID is required.");

        RuleFor(x => x.LocationId)
            .NotEqual(Guid.Empty)
            .When(x => x.LocationId.HasValue)
            .WithMessage("Location ID cannot be empty if specified.");
    }
}
