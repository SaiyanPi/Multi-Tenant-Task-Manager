using System.Security.Claims;
using System.Text.Json;
using DTOs.Comment;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using MultiTenantTaskManager.Accessor;
using MultiTenantTaskManager.Authentication;
using MultiTenantTaskManager.Data;
using MultiTenantTaskManager.DTOs.Comment;
using MultiTenantTaskManager.DTOs.Notification;
using MultiTenantTaskManager.Hubs;
using MultiTenantTaskManager.Mappers;

namespace MultiTenantTaskManager.Services;

public class CommentService : TenantAwareService, ICommentService
{

    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IUserAccessor _userAccessor;
    private readonly IAuditService _auditService;
    private readonly INotificationService _notificationService;

    public CommentService(
        ApplicationDbContext context,
        ClaimsPrincipal user,
        ITenantAccessor tenantAccessor,
        IAuthorizationService authorizationService,
        UserManager<ApplicationUser> userManager,
        IUserAccessor userAccessor,
        IAuditService auditService,
        INotificationService notificationService)
        : base(user, tenantAccessor, authorizationService)
    {
        _context = context;
        _userManager = userManager;
        _userAccessor = userAccessor;
        _auditService = auditService;
        _notificationService = notificationService;
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

            // ------ trigger comment notification ---------

            // Step 1: Listing target users to notify
            List<string> targetUserIds = new();

            if (dto.TaskItemId.HasValue)
            {
                targetUserIds = await _context.TaskItems
                    .Where(t => t.Id == dto.TaskItemId)
                    .Select(t => t.AssignedUserId)
                    .Where(id => id != null && id != userId)
                    .Cast<string>()
                    .ToListAsync();
            }

            else if (dto.ProjectId.HasValue)
            {
                targetUserIds = await _context.Projects
                    .Where(p => p.Id == dto.ProjectId)
                    .SelectMany(p => p.AssignedUsers)
                    .Where(user => user.Id != userId)
                    .Select(user => user.Id)
                    .ToListAsync();
            }

            // Step 2: Send real-time notification
            var SenderName = await _context.Users
                .Where(u => u.Id == userId && u.TenantId == tenantId && !u.IsDeleted)
                .Select(u => u.Email)
                .FirstOrDefaultAsync();

            var taskItemName = await _context.TaskItems
                .Where(t => t.Id == dto.TaskItemId && t.TenantId == tenantId && !t.IsDeleted)
                .Select(p => p.Titles)
                .FirstOrDefaultAsync();
            var projectName = await _context.Projects
                .Where(p => p.Id == dto.ProjectId && p.TenantId == tenantId && !p.IsDeleted)
                .Select(p => p.Name)
                .FirstOrDefaultAsync();
            
            var commentDto = new CommentNotificationDto
            {
                CommentId = comment.Id,
                Title = "New Comment Added",
                SenderName = SenderName ?? "Unknown",
                Content = comment.Content,  // for displaying in notification
                Message = "New Comment",    // for saving in db
                TaskItemId = comment.TaskItemId,
                TaskName = taskItemName,
                ProjectName = projectName,
                ProjectId = comment.ProjectId,
                Type = "comment"  // this is already set in the constructor of CommentNotificationDto
            };

Console.WriteLine(JsonSerializer.Serialize(commentDto));
            // call SignalR notification
            foreach (var targetUserId in targetUserIds)
            {
                await _notificationService.SendNotificationAsync(targetUserId, commentDto);
            }

            // --------------------------------------


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
        await AuthorizeSameTenantAsync();
        var tenantId = GetCurrentTenantId();
        var userId = _userAccessor.UserId.ToString();

        var comment = await _context.Comments
            .FirstOrDefaultAsync(c => c.Id == commentId && c.TenantId == tenantId);
        if (comment == null)
            return false;

        if( comment.UserId != userId )
            throw new Exception("You are not the author of this comment");

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