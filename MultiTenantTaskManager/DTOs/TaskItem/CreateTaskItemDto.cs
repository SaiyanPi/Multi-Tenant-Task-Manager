namespace MultiTenantTaskManager.DTOs.TaskItem;

public class CreateTaskItemDto
{
    public string Titles { get; set; } = string.Empty;

    // Foreign key to Project
    public int ProjectId { get; set; }
    public string Description { get; set; } = string.Empty;

    public DateTime? DueDate { get; set; }

}