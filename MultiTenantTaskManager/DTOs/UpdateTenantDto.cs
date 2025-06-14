namespace  MultiTenantTaskManager.DTOs;

public class UpdateTenantDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Domain { get; set; }
}