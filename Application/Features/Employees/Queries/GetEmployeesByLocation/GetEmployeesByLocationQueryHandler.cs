using Application.Common.Interfaces;
using Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Employees.Queries.GetEmployeesByLocation;

public sealed class GetEmployeesByLocationQueryHandler
    : IHandler<GetEmployeesByLocationQuery, Result<IReadOnlyList<EmployeeDto>>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IPermissionService _permissionService;

    public GetEmployeesByLocationQueryHandler(IApplicationDbContext context, ICurrentUserService currentUserService, IPermissionService permissionService)
    {
        _context = context;
        _currentUserService = currentUserService;
        _permissionService = permissionService;
    }

    public async Task<Result<IReadOnlyList<EmployeeDto>>> HandleAsync(
        GetEmployeesByLocationQuery request,
        CancellationToken cancellationToken = default)
    {
        // Authorize: Check if current user has permission
        var currentUsername = await _currentUserService.GetUsernameAsync();
        if (string.IsNullOrEmpty(currentUsername))
        {
            return Result.Failure<IReadOnlyList<EmployeeDto>>(new Error("Security.Unauthenticated", "User is not authenticated."));
        }

        var isAuthorized = await _permissionService.HasPermissionAsync(currentUsername, "Employee.View", cancellationToken: cancellationToken);

        if (!isAuthorized)
        {
            return Result.Failure<IReadOnlyList<EmployeeDto>>(new Error("Security.Unauthorized", "You do not have permission to view employees."));
        }

        var employees = await _context.Employees
            .AsNoTracking()
            .Where(e => e.LocationId == request.LocationId && e.IsActive)
            .OrderBy(e => e.Name)
            .Select(e => new EmployeeDto(e.Id, e.Name, e.Username))
            .ToListAsync(cancellationToken);

        return Result.Success<IReadOnlyList<EmployeeDto>>(employees);
    }
}
