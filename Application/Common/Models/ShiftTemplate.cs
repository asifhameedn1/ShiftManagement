namespace Application.Common.Models;

/// <summary>
/// Predefined shift templates available for assignment.
/// Static constants — add a ShiftTemplate DB table later if admin-configurability is needed.
/// </summary>
public sealed record ShiftTemplate(string Name, TimeOnly StartTime, TimeOnly EndTime)
{
    public static readonly ShiftTemplate Morning   = new("Morning",   new TimeOnly(6,  0), new TimeOnly(14, 0));
    public static readonly ShiftTemplate Afternoon = new("Afternoon", new TimeOnly(14, 0), new TimeOnly(22, 0));
    public static readonly ShiftTemplate Night     = new("Night",     new TimeOnly(22, 0), new TimeOnly(6,  0));

    public static IReadOnlyList<ShiftTemplate> All => [Morning, Afternoon, Night];
}
