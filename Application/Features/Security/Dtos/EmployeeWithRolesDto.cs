using System.Collections.Generic;

namespace Application.Features.Security.Dtos;

public sealed record EmployeeWithRolesDto(
    Guid Id,
    string Name,
    string Username,
    IReadOnlyList<RoleDto> Roles);
