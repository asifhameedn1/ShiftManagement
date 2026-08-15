using Application.Common.Interfaces;
using Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Employees.Queries.GetEmployeesByLocation;

public sealed class GetEmployeesByLocationQueryHandler
    : IHandler<GetEmployeesByLocationQuery, Result<IReadOnlyList<EmployeeDto>>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetEmployeesByLocationQueryHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
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

        var isAuthorized = await _context.Employees
            .Where(e => e.Username == currentUsername && e.IsActive)
            .SelectMany(e => e.Roles)
            .SelectMany(r => r.Permissions)
            .AnyAsync(p => p.Name == "Employee.View", cancellationToken);

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
