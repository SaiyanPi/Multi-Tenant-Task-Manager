namespace MultiTenantTaskManager.DTOs.TaskItem;
public class AssignTaskDto
{
    public int TaskItemId { get; set; }
    public string AssignedUserId { get; set; } = string.Empty;

}