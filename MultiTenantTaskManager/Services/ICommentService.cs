using DTOs.Comment;
using MultiTenantTaskManager.DTOs.Comment;

namespace MultiTenantTaskManager.Services;
public interface ICommentService
{
    Task<CommentDto> AddCommentAsync(CreateCommentDto dto, string userId, Guid tenantId);
    Task<CommentDto> UpdateCommentAsync(Guid commentId, string userId, UpdateCommentDto dto);
    Task<bool> DeleteCommentAsync(Guid commentId);
    Task<IEnumerable<CommentDto>> GetCommentsByTaskItemIdAsync(int taskItemId, Guid tenantId);
    Task<IEnumerable<CommentDto>> GetCommentsByProjectIdAsync(int projectId, Guid tenantId);
}