using Domain.Common;

namespace Application.Features.Departments.Queries.GetDepartmentsWithLocations;

/// <summary>
/// Returns departments with their nested locations. When <paramref name="RequiredPermission"/> is
/// set, only departments in which the current user holds that permission are returned.
/// </summary>
public sealed record GetDepartmentsWithLocationsQuery(string? RequiredPermission = null);

public sealed record DepartmentDto(Guid Id, string Name, IReadOnlyList<LocationDto> Locations);

public sealed record LocationDto(Guid Id, string Name, int EmployeeCount);
