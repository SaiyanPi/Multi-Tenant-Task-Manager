using DTOs.Comment;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MultiTenantTaskManager.Accessor;
using MultiTenantTaskManager.DTOs.Comment;
using MultiTenantTaskManager.Services;

namespace MultiTenantTaskManager.Controllers;

// Unlike other services userId and tenantId are extracted here in the controller via accessor 
// because why not?
// ⚠️ If you are someone referencing my code, please follow the pattern by injecting them directly into
// the service instead.)

[ApiController]
[Route("api/[controller]")]
public class CommentsController : ControllerBase
{
    private readonly ICommentService _commentService;
    private readonly IUserAccessor _userAccessor;
    private readonly ITenantAccessor _tenantAccessor;

    public CommentsController(ICommentService commentService, IUserAccessor userAccessor,
        ITenantAccessor tenantAccessor)
    {
        _commentService = commentService ?? throw new ArgumentNullException(nameof(commentService));
        _userAccessor = userAccessor ?? throw new ArgumentNullException(nameof(userAccessor));
        _tenantAccessor = tenantAccessor ?? throw new ArgumentNullException(nameof(tenantAccessor));
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> AddComment([FromBody] CreateCommentDto dto)
    {
        var userId = _userAccessor.UserId.ToString();
        var tenantId = _tenantAccessor.TenantId;
        var comment = await _commentService.AddCommentAsync(dto, userId, tenantId);
        return Ok(comment);
    }

    [Authorize]
    [HttpPut("{commentId}")]
    public async Task<IActionResult> UpdateComment(Guid commentId, [FromBody] UpdateCommentDto dto)
    {
        var userId = _userAccessor.UserId.ToString();
        var comment = await _commentService.UpdateCommentAsync(commentId, userId, dto);
        return Ok(comment);
    }

    [Authorize]
    [HttpDelete("{commentId}")]
    public async Task<IActionResult> DeleteComment(Guid commentId)
    {
        var success = await _commentService.DeleteCommentAsync(commentId);
        if (!success) return NotFound($"Comment with ID {commentId} not found.");
        return NoContent();
    }

    [Authorize(Policy = "canViewTasks")]
    [HttpGet("task/{taskItemId}")]
    public async Task<ActionResult<IEnumerable<CommentDto>>> GetCommentsByTaskItemId(int taskItemId)
    {
        var tenantId = _tenantAccessor.TenantId;
        var comments = await _commentService.GetCommentsByTaskItemIdAsync(taskItemId, tenantId);
        return Ok(comments);
    }

    [Authorize(Policy = "canViewTasks")]
    [HttpGet("project/{projectId}")]
    public async Task<ActionResult<IEnumerable<CommentDto>>> GetCommentsByProjectId(int projectId)
    {
        var tenantId = _tenantAccessor.TenantId;
        var comments = await _commentService.GetCommentsByProjectIdAsync(projectId, tenantId);
        return Ok(comments);
    }
}