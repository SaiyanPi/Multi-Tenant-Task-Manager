namespace MultiTenantTaskManager.DTOs;
public class CreateTenantDto
{
    public string Name { get; set; } = string.Empty;
    public string? Domain { get; set; }
}