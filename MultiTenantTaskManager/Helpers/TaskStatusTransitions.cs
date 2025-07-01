using MultiTenantTaskManager.Enums;

namespace MultiTenantTaskManager.Helpers;

public static class TaskStatusTransition
{
    private static readonly Dictionary<TaskItemStatus, TaskItemStatus[]> _validTransitions = new()
    {
        [TaskItemStatus.Unassigned] = new[] { TaskItemStatus.Assigned },
        [TaskItemStatus.Assigned] = new[] { TaskItemStatus.InProgress },
        [TaskItemStatus.InProgress] = new[] { TaskItemStatus.Completed },
        [TaskItemStatus.Completed] = Array.Empty<TaskItemStatus>() // Final state
    };

    public static bool CanTransition(TaskItemStatus current, TaskItemStatus next)
    {
        return _validTransitions.TryGetValue(current, out var allowed) && allowed.Contains(next);
    }
}

