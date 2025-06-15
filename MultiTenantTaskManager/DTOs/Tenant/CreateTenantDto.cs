namespace MultiTenantTaskManager.DTOs.Tenant;
public class CreateTenantDto
{
    public string Name { get; set; } = string.Empty;
    public string? Domain { get; set; }
}