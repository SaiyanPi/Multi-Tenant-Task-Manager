using DTOs.Comment;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MultiTenantTaskManager.Accessor;
using MultiTenantTaskManager.Authentication;
using MultiTenantTaskManager.Data;
using MultiTenantTaskManager.DTOs.Comment;
using MultiTenantTaskManager.Mappers;

namespace MultiTenantTaskManager.Services;

public class CommentService : ICommentService
{

    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IUserAccessor _userAccessor;

    public CommentService(ApplicationDbContext context, UserManager<ApplicationUser> userManager,
        IUserAccessor userAccessor)
    {
        _context = context;
        _userManager = userManager;
        _userAccessor = userAccessor;
    }

    public async Task<CommentDto> AddCommentAsync(CreateCommentDto dto, string userId, Guid tenantId)
    {
        if (dto == null) throw new ArgumentNullException(nameof(dto));

        var comment = dto.ToCommentModel();
        comment.UserId = userId;
        comment.TenantId = tenantId;

        await _context.Comments.AddAsync(comment);
        await _context.SaveChangesAsync();

        return CommentMapper.ToCommentDto(comment);
    }

    public async Task<CommentDto> UpdateCommentAsync(Guid commentId, string userId, UpdateCommentDto dto)
    {
        if (dto == null) throw new ArgumentNullException(nameof(dto));

        var comment = await _context.Comments.FindAsync(commentId);
        if (comment == null)
            throw new Exception("Comment not found");

        // Only allow the original author to update the comment
        if (comment.UserId != userId)
            throw new Exception("You do not have permission to update this comment");

        comment.Content = dto.Content;
        comment.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return CommentMapper.ToCommentDto(comment);
    }

    public async Task<bool> DeleteCommentAsync(Guid commentId)
    {
        var comment = await _context.Comments.FindAsync(commentId);
        if (comment == null)
            return false;

        comment.IsDeleted = true;
        comment.DeletedAt = DateTime.UtcNow;
        comment.DeletedBy = _userAccessor.UserName ?? "Unknown";

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<IEnumerable<CommentDto>> GetCommentsByTaskItemIdAsync(int taskItemId, Guid tenantId)
    {
        var comments = await _context.Comments
            .Where(c => c.TaskItemId == taskItemId && c.TenantId == tenantId && !c.IsDeleted)
            .Include(c => c.User) // including user for userName in CommentMapper
            .OrderBy(c => c.CreatedAt)
            .ToListAsync();

        return comments.Select(CommentMapper.ToCommentDto);
    }

    public async Task<IEnumerable<CommentDto>> GetCommentsByProjectIdAsync(int projectId, Guid tenantId)
    {
        var comments = await _context.Comments
            .Where(c => c.ProjectId == projectId && c.TenantId == tenantId && !c.IsDeleted)
            .Include(c => c.User)
            .OrderBy(c => c.CreatedAt)
            .ToListAsync();

        return comments.Select(CommentMapper.ToCommentDto);
    }

}