using MultiTenantTaskManager.DTOs.Project;

namespace MultiTenantTaskManager.DTOs.Tenant;

public class TenantDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Domain { get; set; }
    public ICollection<ProjectDto>? Projects { get; set; }
    //public ICollection<UserDto>? Users { get; set; }
}