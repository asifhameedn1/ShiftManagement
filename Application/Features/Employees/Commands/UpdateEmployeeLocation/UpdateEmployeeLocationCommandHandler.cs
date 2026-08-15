using Application.Common.Interfaces;
using Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Employees.Commands.UpdateEmployeeLocation;

public sealed class UpdateEmployeeLocationCommandHandler
    : IHandler<UpdateEmployeeLocationCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public UpdateEmployeeLocationCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<Result> HandleAsync(
        UpdateEmployeeLocationCommand request,
        CancellationToken cancellationToken = default)
    {
        // Authorize: Check if current user has permission
        var currentUsername = await _currentUserService.GetUsernameAsync();
        if (string.IsNullOrEmpty(currentUsername))
        {
            return Result.Failure(new Error("Security.Unauthenticated", "User is not authenticated."));
        }

        var isAuthorized = await _context.Employees
            .Where(e => e.Username == currentUsername && e.IsActive)
            .SelectMany(e => e.Roles)
            .SelectMany(r => r.Permissions)
            .AnyAsync(p => p.Name == "Employee.Manage", cancellationToken);

        if (!isAuthorized)
        {
            return Result.Failure(new Error("Security.Unauthorized", "You do not have permission to manage employees."));
        }

        // 1. Fetch employee
        var employee = await _context.Employees
            .FirstOrDefaultAsync(e => e.Id == request.EmployeeId, cancellationToken);

        if (employee is null)
        {
            return Result.Failure(
                Error.NotFound("Employee", $"Employee '{request.EmployeeId}' was not found."));
        }

        // 2. Assign/Unassign Location
        if (request.LocationId.HasValue)
        {
            var locationExists = await _context.Locations
                .AnyAsync(l => l.Id == request.LocationId.Value, cancellationToken);

            if (!locationExists)
            {
                return Result.Failure(
                    Error.NotFound("Location", $"Location '{request.LocationId.Value}' was not found."));
            }

            employee.AssignLocation(request.LocationId.Value);
        }
        else
        {
            employee.UnassignLocation();
        }

        // 3. Save changes
        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
