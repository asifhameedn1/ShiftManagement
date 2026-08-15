using Domain.Common;

namespace Application.Features.Shifts.Queries.GetShifts;

/// <summary>Returns all shifts, optionally filtered by employee.</summary>
public sealed record GetShiftsQuery(Guid? EmployeeId = null);

/// <summary>Lightweight read model returned by the query.</summary>
public sealed record ShiftDto(
    Guid Id,
    Guid EmployeeId,
    string EmployeeName,
    DateOnly Date,
    TimeOnly StartTime,
    TimeOnly EndTime,
    string? Notes);
