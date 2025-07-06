namespace MultiTenantTaskManager.DTOs.Project;

public class UpdateProjectStatusDto
{
    public string NewStatus { get; set; } = default!; // string to control error message cleanly in validation
    
    
}