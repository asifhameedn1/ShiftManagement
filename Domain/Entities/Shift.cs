using Domain.Common;

namespace Domain.Entities;

/// <summary>Represents a work shift assigned to an employee.</summary>
public sealed class Shift
{
    private Shift() { } // EF Core constructor

    public Guid Id { get; private set; }
    public Guid EmployeeId { get; private set; }
    public Employee Employee { get; private set; } = null!;

    public DateOnly Date { get; private set; }
    public TimeOnly StartTime { get; private set; }
    public TimeOnly EndTime { get; private set; }

    public string? Notes { get; private set; }
    public DateTime CreatedAt { get; private set; }

    // Domain errors
    public static class Errors
    {
        public static readonly Error NotFound =
            Error.NotFound("Shift", "The specified shift was not found.");
        public static readonly Error EndBeforeStart =
            Error.Validation("Shift", "EndTime must be after StartTime.");
        public static readonly Error OverlappingShift =
            Error.Conflict("Shift", "The employee already has a shift that overlaps this time range.");
    }

    public static Result<Shift> Create(
        Guid employeeId,
        DateOnly date,
        TimeOnly startTime,
        TimeOnly endTime,
        string? notes = null)
    {
        if (endTime <= startTime)
            return Result.Failure<Shift>(Errors.EndBeforeStart);

        var shift = new Shift
        {
            Id = Guid.NewGuid(),
            EmployeeId = employeeId,
            Date = date,
            StartTime = startTime,
            EndTime = endTime,
            Notes = notes,
            CreatedAt = DateTime.UtcNow
        };

        return Result.Success(shift);
    }

    public Result UpdateTimes(TimeOnly startTime, TimeOnly endTime)
    {
        if (endTime <= startTime)
            return Result.Failure(Errors.EndBeforeStart);

        StartTime = startTime;
        EndTime = endTime;
        return Result.Success();
    }

    public void UpdateNotes(string? notes) => Notes = notes;
}
