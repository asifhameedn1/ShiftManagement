using Domain.Common;
using FluentValidation;

namespace Application.Features.Shifts.Commands.CreateShift;

/// <summary>Command to create a new shift for an employee.</summary>
public sealed record CreateShiftCommand(
    Guid EmployeeId,
    DateOnly Date,
    TimeOnly StartTime,
    TimeOnly EndTime,
    string? Notes = null);

public sealed class CreateShiftCommandValidator : AbstractValidator<CreateShiftCommand>
{
    public CreateShiftCommandValidator()
    {
        RuleFor(x => x.EmployeeId)
            .NotEmpty()
            .WithMessage("EmployeeId is required.");

        RuleFor(x => x.Date)
            .NotEmpty()
            .WithMessage("Date is required.");

        RuleFor(x => x.StartTime)
            .NotEmpty()
            .WithMessage("StartTime is required.");

        RuleFor(x => x.EndTime)
            .GreaterThan(x => x.StartTime)
            .WithMessage("EndTime must be after StartTime.");

        RuleFor(x => x.Notes)
            .MaximumLength(500)
            .When(x => x.Notes is not null);
    }
}
