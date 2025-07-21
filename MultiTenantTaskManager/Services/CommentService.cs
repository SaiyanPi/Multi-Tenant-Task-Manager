using System.Security.Claims;
using System.Text.Json;
using DTOs.Comment;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MultiTenantTaskManager.Accessor;
using MultiTenantTaskManager.Authentication;
using MultiTenantTaskManager.Data;
using MultiTenantTaskManager.DTOs.Comment;
using MultiTenantTaskManager.Mappers;

namespace MultiTenantTaskManager.Services;

public class CommentService : TenantAwareService, ICommentService
{

    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IUserAccessor _userAccessor;
    private readonly IAuditService _auditService;

    public CommentService(
        ApplicationDbContext context,
        ClaimsPrincipal user,
        ITenantAccessor tenantAccessor,
        IAuthorizationService authorizationService,
        UserManager<ApplicationUser> userManager,
        IUserAccessor userAccessor,
        IAuditService auditService) 
        : base(user, tenantAccessor, authorizationService)
    {
        _context = context;
        _userManager = userManager;
        _userAccessor = userAccessor;
        _auditService = auditService;
    }

    public async Task<CommentDto> AddCommentAsync(CreateCommentDto dto, string userId, Guid tenantId)
    {
        if (dto == null) throw new ArgumentNullException(nameof(dto));

        await AuthorizeSameTenantAsync();

        // XOR logic
        if (dto.TaskItemId.HasValue ^ dto.ProjectId.HasValue)
        {
            if (dto.TaskItemId.HasValue)
            {
                var taskExist = await _context.TaskItems
                    .AnyAsync(t => t.Id == dto.TaskItemId && t.TenantId == tenantId && !t.IsDeleted);
                if (!taskExist)
                    throw new Exception("TaskItem not found");

                // Create comment linked to task
            }
            else if (dto.ProjectId.HasValue)
            {
                var projectExist = await _context.Projects
                    .AnyAsync(p => p.Id == dto.ProjectId && p.TenantId == tenantId && !p.IsDeleted);
                if (!projectExist)
                    throw new Exception("Project not found");

                // Create comment linked to project
            }

            var comment = dto.ToCommentModel();
            comment.UserId = userId;
            comment.TenantId = tenantId;

            await _context.Comments.AddAsync(comment);
            await _context.SaveChangesAsync();

            await _auditService.LogAsync(
                action: "Create",
                entityName: "Comment",
                entityId: comment.Id.ToString(),
                changes: JsonSerializer.Serialize(CommentMapper.ToCommentDto(comment))
            );

            return CommentMapper.ToCommentDto(comment);
        }
        else
        {
            throw new Exception("Comment must belong to either a task or a project, but not both.");
        }
    }

    public async Task<CommentDto> UpdateCommentAsync(Guid commentId, string userId, UpdateCommentDto dto)
    {
        if (dto == null) throw new ArgumentNullException(nameof(dto));

        await AuthorizeSameTenantAsync();
        var tenantId = GetCurrentTenantId();

        var existingComment = await _context.Comments
            .FirstOrDefaultAsync(c => c.Id == commentId && c.TenantId == tenantId);

        if (existingComment == null)
            throw new Exception("Comment not found");

        // original comment DTO before the update
        var originalCommentDto = CommentMapper.ToCommentDto(existingComment);

        // now retrieve it again for tracking changes
        var trackedComment = await _context.Comments
            .FirstOrDefaultAsync(c => c.Id == commentId && c.TenantId == tenantId);

        // Only allow the original author to update the comment
        // if (comment.UserId != userId)
        //     throw new Exception("You do not have permission to update this comment");

        // comment.Content = dto.Content;
        // comment.UpdatedAt = DateTime.UtcNow;

        if (trackedComment == null)
            throw new Exception("Comment not found");

        if (trackedComment.UserId != userId)
            throw new Exception("You do not have permission to update this comment");

        trackedComment.Content = dto.Content;
        trackedComment.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        // comment DTO after the update
        var updatedCommentDto = CommentMapper.ToCommentDto(trackedComment);

        // log both old and new state
        var auditData = new
        {
            Original = originalCommentDto,
            Updated = updatedCommentDto
        };

        await _auditService.LogAsync(
            action: "UpdateComment",
            entityName: "Comment",
            entityId: commentId.ToString(),
            changes: JsonSerializer.Serialize(auditData)
        );

        return CommentMapper.ToCommentDto(trackedComment);
    }

    public async Task<bool> DeleteCommentAsync(Guid commentId)
    {
        var comment = await _context.Comments.FindAsync(commentId);
        if (comment == null)
            return false;

        // DTO before deletion
        var deletedCommentDto = CommentMapper.ToCommentDto(comment);

        comment.IsDeleted = true;
        comment.DeletedAt = DateTime.UtcNow;
        comment.DeletedBy = _userAccessor.UserName ?? "Unknown";

        await _context.SaveChangesAsync();

        // audit log after deletion
        await _auditService.LogAsync(
            action: "DeleteComment",
            entityName: "Comment",
            entityId: commentId.ToString(),
            changes: JsonSerializer.Serialize(deletedCommentDto)
        );

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