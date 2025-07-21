namespace DTOs.Comment;

public class CommentDto
{
    public Guid Id { get; set; }
    public string Content { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public Guid? TenantId { get; set; } //
    public string UserName { get; set; } = string.Empty; 
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // nullable to support polymorphic linking
    // validation will enforce "exactly one of these must be set"
    public int? TaskItemId { get; set; }
    public int? ProjectId { get; set; }
}