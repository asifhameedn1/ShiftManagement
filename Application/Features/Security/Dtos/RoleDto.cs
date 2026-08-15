using System.Collections.Generic;

namespace Application.Features.Security.Dtos;

public sealed record RoleDto(Guid Id, string Name, IReadOnlyList<PermissionDto> Permissions);
