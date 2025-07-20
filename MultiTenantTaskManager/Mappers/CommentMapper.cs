using DTOs.Comment;
using MultiTenantTaskManager.DTOs.Comment;
using MultiTenantTaskManager.Models;

namespace MultiTenantTaskManager.Mappers;

public static class CommentMapper
{
    public static CommentDto ToCommentDto(this Comment comment)
    {
        return new CommentDto
        {
            Id = comment.Id,
            Content = comment.Content,
            UserId = comment.UserId,
            UserName = comment.User?.UserName ?? string.Empty, 
            CreatedAt = comment.CreatedAt,
            UpdatedAt = comment.UpdatedAt,
            TaskItemId = comment.TaskItemId,
            ProjectId = comment.ProjectId
        };
    }

    public static Comment ToCommentModel(this CreateCommentDto dto)
    {
        return new Comment
        {
            Content = dto.Content,
            TaskItemId = dto.TaskItemId,
            ProjectId = dto.ProjectId
        };
    }

    public static void UpdateFromDto(this Comment comment, UpdateCommentDto dto)
    {
        if (dto == null) throw new ArgumentNullException(nameof(dto));

        comment.Content = dto.Content;
        comment.UpdatedAt = DateTime.UtcNow;


    }
}
