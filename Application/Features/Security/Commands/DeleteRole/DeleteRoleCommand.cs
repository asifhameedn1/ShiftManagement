using Domain.Common;
using System;

namespace Application.Features.Security.Commands.DeleteRole;

public sealed record DeleteRoleCommand(Guid Id);
