namespace MultiTenantTaskManager.DTOs.TaskItem;

public class UpdateTaskItemDto
{
    public int Id { get; set; }
    public string Titles { get; set; } = string.Empty;

    // Foreign key to Project
    public int ProjectId { get; set; }
    public DateTime? DueDate { get; set; }
}
