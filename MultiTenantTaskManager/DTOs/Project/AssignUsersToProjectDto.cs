namespace MultiTenantTaskManager.DTOs.Project;
public class AssignUsersToProjectDto
{
    public int ProjectId { get; set; }

    // Key = userId, Value = role (Member or SpecialMember)
    public Dictionary<string, string> AssignedUsers { get; set; } = new();
}