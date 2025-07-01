using MultiTenantTaskManager.Enums;

namespace MultiTenantTaskManager.DTOs.TaskItem;

public class UpdateTaskItemStatusDto
{
    // public TaskItemStatus NewStatus { get; set; }
    public string NewStatus { get; set; } = default!; // string to control error message cleanly in validation
    
    
}