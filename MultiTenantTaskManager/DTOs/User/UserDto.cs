namespace MultiTenantTaskManager.DTOs.User;
public class UserDto
{
    public string Id { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public Guid TenantId { get; set; }
    public List<string> Roles { get; set; } = new List<string>();

    // Additional properties can be added as needed
}