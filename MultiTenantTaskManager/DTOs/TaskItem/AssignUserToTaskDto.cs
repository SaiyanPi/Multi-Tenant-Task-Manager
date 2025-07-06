namespace MultiTenantTaskManager.DTOs.TaskItem;
public class AssignUserToTaskDto
{
    public int TaskItemId { get; set; }
    public string AssignedUser { get; set; } = string.Empty;

}