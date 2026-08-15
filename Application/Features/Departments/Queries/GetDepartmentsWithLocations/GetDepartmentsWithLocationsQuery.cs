using Domain.Common;

namespace Application.Features.Departments.Queries.GetDepartmentsWithLocations;

/// <summary>Returns all departments with their nested locations.</summary>
public sealed record GetDepartmentsWithLocationsQuery;

public sealed record DepartmentDto(Guid Id, string Name, IReadOnlyList<LocationDto> Locations);

public sealed record LocationDto(Guid Id, string Name, int EmployeeCount);
