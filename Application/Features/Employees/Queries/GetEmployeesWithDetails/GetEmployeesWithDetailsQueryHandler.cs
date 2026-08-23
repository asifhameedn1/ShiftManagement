using Application.Common.Interfaces;
using Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Employees.Queries.GetEmployeesWithDetails;

public sealed class GetEmployeesWithDetailsQueryHandler
    : IHandler<GetEmployeesWithDetailsQuery, Result<IReadOnlyList<EmployeeWithDetailsDto>>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IPermissionService _permissionService;

    public GetEmployeesWithDetailsQueryHandler(IApplicationDbContext context, ICurrentUserService currentUserService, IPermissionService permissionService)
    {
        _context = context;
        _currentUserService = currentUserService;
        _permissionService = permissionService;
    }

    public async Task<Result<IReadOnlyList<EmployeeWithDetailsDto>>> HandleAsync(
        GetEmployeesWithDetailsQuery request,
        CancellationToken cancellationToken = default)
    {
        // Authorize: Check if current user has permission
        var currentUsername = await _currentUserService.GetUsernameAsync();
        if (string.IsNullOrEmpty(currentUsername))
        {
            return Result.Failure<IReadOnlyList<EmployeeWithDetailsDto>>(new Error("Security.Unauthenticated", "User is not authenticated."));
        }

        var isAuthorized = await _permissionService.HasPermissionAsync(currentUsername, "Employee.View", cancellationToken: cancellationToken);

        if (!isAuthorized)
        {
            return Result.Failure<IReadOnlyList<EmployeeWithDetailsDto>>(new Error("Security.Unauthorized", "You do not have permission to view employees."));
        }

        var employees = await _context.Employees
            .AsNoTracking()
            .Include(e => e.Location)
                .ThenInclude(l => l!.Department)
            .Where(e => e.IsActive)
            .OrderBy(e => e.Name)
            .Select(e => new EmployeeWithDetailsDto(
                e.Id,
                e.Name,
                e.Username,
                e.LocationId,
                e.Location != null ? e.Location.Name : null,
                e.Location != null ? e.Location.DepartmentId : null,
                e.Location != null && e.Location.Department != null ? e.Location.Department.Name : null,
                e.IsActive))
            .ToListAsync(cancellationToken);

        return Result.Success<IReadOnlyList<EmployeeWithDetailsDto>>(employees);
    }
}
