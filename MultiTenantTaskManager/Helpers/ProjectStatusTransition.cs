using MultiTenantTaskManager.Enums;

namespace MultiTenantTaskManager.Helpers;

public static class ProjectStatusTransition
{
    private static readonly Dictionary<ProjectStatus, ProjectStatus[]> _validTransitions = new()
    {
        [ProjectStatus.Unassigned] = new[] { ProjectStatus.Assigned },
        [ProjectStatus.Assigned] = new[] { ProjectStatus.InProgress },
        [ProjectStatus.InProgress] = new[] { ProjectStatus.Completed },
        [ProjectStatus.Completed] = Array.Empty<ProjectStatus>() // Final state
    };

    public static bool CanTransition(ProjectStatus current, ProjectStatus next)
    {
        return _validTransitions.TryGetValue(current, out var allowed) && allowed.Contains(next);
    }
}

