namespace MultiTenantTaskManager.DTOs.Comment;
public class CreateCommentDto
{
    public string Content { get; set; } = string.Empty;

    // nullable to support polymorphic linking
    // validation will enforce "exactly one of these must be set"
    public int? TaskItemId { get; set; }
    public int? ProjectId { get; set; }
}