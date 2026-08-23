using System.Collections.Generic;

namespace Application.Features.Security.Dtos;

public sealed record DepartmentRoleAssignmentDto(
    Guid DepartmentId,
    string DepartmentName,
    IReadOnlyList<RoleDto> Roles);
