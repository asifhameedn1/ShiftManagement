using Application.Common.Interfaces;
using Domain.Common;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Shifts.Commands.CreateShift;

public sealed class CreateShiftCommandHandler
    : IHandler<CreateShiftCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IPermissionService _permissionService;

    public CreateShiftCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService, IPermissionService permissionService)
    {
        _context = context;
        _currentUserService = currentUserService;
        _permissionService = permissionService;
    }

    public async Task<Result<Guid>> HandleAsync(
        CreateShiftCommand request,
        CancellationToken cancellationToken = default)
    {
        // Authorize: Check if current user has permission
        var currentUsername = await _currentUserService.GetUsernameAsync();
        if (string.IsNullOrEmpty(currentUsername))
        {
            return Result.Failure<Guid>(new Error("Security.Unauthenticated", "User is not authenticated."));
        }

        var isAuthorized = await _permissionService.HasPermissionAsync(currentUsername, "Shift.Manage", cancellationToken: cancellationToken);

        if (!isAuthorized)
        {
            return Result.Failure<Guid>(new Error("Security.Unauthorized", "You do not have permission to manage shifts."));
        }

        // Verify employee exists
        var employeeExists = await _context.Employees
            .AnyAsync(e => e.Id == request.EmployeeId && e.IsActive, cancellationToken);

        if (!employeeExists)
            return Result.Failure<Guid>(
                Error.NotFound("Employee", $"Employee '{request.EmployeeId}' was not found or is inactive."));

        // Check for overlapping shifts on the same day
        var hasOverlap = await _context.Shifts.AnyAsync(s =>
            s.EmployeeId == request.EmployeeId &&
            s.Date == request.Date &&
            s.StartTime < request.EndTime &&
            s.EndTime > request.StartTime,
            cancellationToken);

        if (hasOverlap)
            return Result.Failure<Guid>(Shift.Errors.OverlappingShift);

        // Create via domain factory (validates business rules)
        var shiftResult = Shift.Create(
            request.EmployeeId,
            request.Date,
            request.StartTime,
            request.EndTime,
            request.Notes);

        if (shiftResult.IsFailure)
            return Result.Failure<Guid>(shiftResult.Error);

        _context.Shifts.Add(shiftResult.Value);
        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success(shiftResult.Value.Id);
    }
}
