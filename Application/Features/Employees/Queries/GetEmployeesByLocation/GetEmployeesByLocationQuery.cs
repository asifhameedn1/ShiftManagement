using Domain.Common;

namespace Application.Features.Employees.Queries.GetEmployeesByLocation;

/// <summary>Returns all active employees assigned to a given location.</summary>
public sealed record GetEmployeesByLocationQuery(Guid LocationId);

public sealed record EmployeeDto(Guid Id, string Name, string Username);
