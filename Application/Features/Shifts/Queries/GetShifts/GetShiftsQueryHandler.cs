using Application.Common.Interfaces;
using Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Shifts.Queries.GetShifts;

public sealed class GetShiftsQueryHandler
    : IHandler<GetShiftsQuery, Result<IReadOnlyList<ShiftDto>>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetShiftsQueryHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<Result<IReadOnlyList<ShiftDto>>> HandleAsync(
        GetShiftsQuery request,
        CancellationToken cancellationToken = default)
    {
        // Authorize: Check if current user has permission
        var currentUsername = await _currentUserService.GetUsernameAsync();
        if (string.IsNullOrEmpty(currentUsername))
        {
            return Result.Failure<IReadOnlyList<ShiftDto>>(new Error("Security.Unauthenticated", "User is not authenticated."));
        }

        var isAuthorized = await _context.Employees
            .Where(e => e.Username == currentUsername && e.IsActive)
            .SelectMany(e => e.Roles)
            .SelectMany(r => r.Permissions)
            .AnyAsync(p => p.Name == "Shift.View", cancellationToken);

        if (!isAuthorized)
        {
            return Result.Failure<IReadOnlyList<ShiftDto>>(new Error("Security.Unauthorized", "You do not have permission to view shifts."));
        }

        var query = _context.Shifts
            .Include(s => s.Employee)
            .AsNoTracking();

        if (request.EmployeeId.HasValue)
            query = query.Where(s => s.EmployeeId == request.EmployeeId.Value);

        var shifts = await query
            .OrderBy(s => s.Date)
            .ThenBy(s => s.StartTime)
            .Select(s => new ShiftDto(
                s.Id,
                s.EmployeeId,
                s.Employee.Name,
                s.Date,
                s.StartTime,
                s.EndTime,
                s.Notes))
            .ToListAsync(cancellationToken);

        return Result.Success<IReadOnlyList<ShiftDto>>(shifts);
    }
}
