using MultiTenantTaskManager.Authentication;
using MultiTenantTaskManager.Enums;
using MultiTenantTaskManager.Services;

namespace MultiTenantTaskManager.Models;
public class Comment : ISoftDeletable
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }

    public string UserId { get; set; } = string.Empty;
    public ApplicationUser? User { get; set; }  // navigation property

    public string Content { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }


    // for soft deletion
    public bool IsDeleted { get; set; }  
    public DateTime? DeletedAt { get; set; }  
    public string DeletedBy { get; set; }  = string.Empty; 


    public CommentTargetType TargetType { get; set; }


    // Polymorphic Linking
    public int? TaskItemId { get; set; }
    public TaskItem? TaskItem { get; set; }

    public int? ProjectId { get; set; }
    public Project? Project { get; set; }
}