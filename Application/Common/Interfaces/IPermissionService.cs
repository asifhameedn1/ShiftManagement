namespace Application.Common.Interfaces;

/// <summary>
/// Checks whether an employee holds a given permission, either anywhere (departmentId: null)
/// or within a specific department.
/// </summary>
public interface IPermissionService
{
    Task<bool> HasPermissionAsync(
        string username,
        string permissionName,
        Guid? departmentId = null,
        CancellationToken cancellationToken = default);

    /// <summary>Returns the ids of the departments in which the employee holds the permission.</summary>
    Task<IReadOnlyList<Guid>> GetDepartmentIdsWithPermissionAsync(
        string username,
        string permissionName,
        CancellationToken cancellationToken = default);
}
