namespace MultiTenantTaskManager.DTOs.TaskItem;
public class AssignUserToTaskDto
{
    public int TaskItemId { get; set; }
    public string AssignedUserId { get; set; } = string.Empty;

}