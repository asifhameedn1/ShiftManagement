using Domain.Common;

namespace Application.Features.Employees.Queries.GetEmployeesWithDetails;

/// <summary>
/// Query to return all active employees with their assigned location and organizational department.
/// </summary>
public sealed record GetEmployeesWithDetailsQuery();

public sealed record EmployeeWithDetailsDto(
    Guid Id,
    string Name,
    string Username,
    Guid? LocationId,
    string? LocationName,
    Guid? DepartmentId,
    string? DepartmentName,
    bool IsActive);
