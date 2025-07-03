using System.Security.Claims;
using System.Text.Json;
using Azure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MultiTenantTaskManager.Accessor;
using MultiTenantTaskManager.Authentication;
using MultiTenantTaskManager.Data;
using MultiTenantTaskManager.DTOs.TaskItem;
using MultiTenantTaskManager.Enums;
using MultiTenantTaskManager.Helpers;
using MultiTenantTaskManager.Mappers;
using MultiTenantTaskManager.Models;
using MultiTenantTaskManager.Validators;

namespace MultiTenantTaskManager.Services;

public class TaskItemService : TenantAwareService, ITaskItemService
{
    private readonly ApplicationDbContext _context;
    private readonly IAuditService _auditService;
    private readonly IUserAccessor _userAccessor;
    private readonly UserManager<ApplicationUser> _userManager;

    public TaskItemService(
        ApplicationDbContext context,
        ClaimsPrincipal user,
        ITenantAccessor tenantAccessor,
        IAuthorizationService authorizationService,
        IAuditService auditService,
        IUserAccessor userAccessor,
        UserManager<ApplicationUser> userManager)
        : base(user, tenantAccessor, authorizationService)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));
        _userAccessor = userAccessor ?? throw new ArgumentNullException(nameof(userAccessor));
        _userManager = userManager;
    }

    public async Task<IEnumerable<TaskItemDto>> GetAllTaskAsync(int page = 1, int pageSize = 10)
    {
        await AuthorizeSameTenantAsync();

        var tenantId = GetCurrentTenantId();

        var tasks = await _context.TaskItems
            .AsNoTracking()
            .Where(t => t.TenantId == tenantId)
            .Include(t => t.AssignedUser)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        // return tasks.Select(TaskItemMapper.ToTaskItemDto);
        return tasks.Select(TaskItemMapper.ToTaskItemDto);
    }

    public async Task<TaskItemDto?> GetTaskByIdAsync(int taskId)
    {
        await AuthorizeSameTenantAsync();

        var tenantId = GetCurrentTenantId();

        var task = await _context.TaskItems
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == taskId && t.TenantId == tenantId);

        return task == null ? null : TaskItemMapper.ToTaskItemDto(task);
        // return task == null ? null : task.ToTaskItemDto();
    }


    public async Task<TaskItemDto> CreateTaskAsync(CreateTaskItemDto dto)
    {
        if (dto == null) throw new ArgumentNullException(nameof(dto));

        await AuthorizeSameTenantAsync();

        var tenantId = GetCurrentTenantId();

        bool taskExist = await _context.TaskItems
            .AnyAsync(p => p.TenantId == tenantId && p.Titles == dto.Titles);
        if (taskExist)
        {
            throw new InvalidOperationException($"A task with title '{dto.Titles}' already exists.");
        }

        var existingProject = await _context.Projects
            .FirstOrDefaultAsync(p => p.Id == dto.ProjectId && p.TenantId == tenantId);

        if (existingProject == null)
        {
            throw new InvalidOperationException($"Project with ID {dto.ProjectId} does not exist.");
        }

        var taskItem = dto.ToTaskItemModel();
        taskItem.TenantId = tenantId; // <-- Enforce correct TenantId

        _context.TaskItems.Add(taskItem);
        await _context.SaveChangesAsync();

        // AuditLog
        // await _auditService.LogAsync("Create", "TaskItem", taskItem.Id.ToString(), JsonSerializer.Serialize(taskItem));
        await _auditService.LogAsync(
            action: "Create",
            entityName: "TaskItem",
            entityId: taskItem.Id.ToString(),
            // following line is commented because taskItem is an EF Core entity — and it still includes
            // navigation properties (like .Project → Tasks → Project...), which causes the serialization
            // cycle. so we have to use DTOs for Logging too.
            // changes: JsonSerializer.Serialize(taskItem),
            changes: JsonSerializer.Serialize(TaskItemMapper.ToTaskItemDto(taskItem))
        );


        return TaskItemMapper.ToTaskItemDto(taskItem);
        // return taskItem.ToTaskItemDto();
    }

    public async Task<TaskItemDto> UpdateTaskAsync(int taskId, UpdateTaskItemDto dto)
    {
        if (dto == null) throw new ArgumentNullException(nameof(dto));

        await AuthorizeSameTenantAsync();

        var tenantId = GetCurrentTenantId();

        // taskItem.TenantId = _tenantAccessor.TenantId; // <-- Enforce correct TenantId
        // var existingTask = await GetTaskByIdAsync(taskId);
        var existingTask = await _context.TaskItems
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == taskId && t.TenantId == tenantId);
        if (existingTask == null)
        {
            throw new KeyNotFoundException($"Task with ID {dto.Id} not found.");
        }

        // original task DTO snapshot before update
        var originalTaskDto = TaskItemMapper.ToTaskItemDto(existingTask);

        // Now retrieve it again for tracking changes
        var trackedTask = await _context.TaskItems
            .FirstAsync(t => t.Id == taskId && t.TenantId == tenantId);

        // existingTask.UpdateFromDto(dto);
        trackedTask.UpdateFromDto(dto);
        await _context.SaveChangesAsync();

        // task DTO after update
        var updatedTaskDto = TaskItemMapper.ToTaskItemDto(trackedTask);

        // Log both old and new state
        var auditData = new
        {
            Original = originalTaskDto,
            Updated = updatedTaskDto
        };

        await _auditService.LogAsync(
            action: "Update",
            entityName: "TaskItem",
            entityId: existingTask.Id.ToString(),
            changes: JsonSerializer.Serialize(auditData)
        );

        return TaskItemMapper.ToTaskItemDto(trackedTask);
        // return existingTask.ToTaskItemDto();
    }

    public async Task<bool> DeleteTaskAsync(int taskId)
    {
        await AuthorizeSameTenantAsync();

        var tenantId = GetCurrentTenantId();

        // var task = await GetTaskByIdAsync(taskId);
        var task = await _context.TaskItems
            .FirstOrDefaultAsync(t => t.Id == taskId && t.TenantId == tenantId);
        if (task == null) return false;

        // task DTO before deletion
        var deletedTaskDto = TaskItemMapper.ToTaskItemDto(task);

        // _context.TaskItems.Remove(task);
        // Soft delete
        task.IsDeleted = true;
        task.DeletedAt = DateTime.UtcNow;
        task.DeletedBy = _userAccessor.UserName ?? "Unknown";

        await _context.SaveChangesAsync();

        // audit Log the after deletion
        await _auditService.LogAsync(
            action: "Delete",
            entityName: "TaskItem",
            entityId: task.Id.ToString(),
            changes: JsonSerializer.Serialize(deletedTaskDto)
        );

        return true;
    }

    public async Task<TaskItemDto> AssignUserToTaskAsync(AssignUserToTaskDto dto)
    {
        var tenantId = _tenantAccessor.TenantId;

        var task = await _context.TaskItems
            .Include(t => t.AssignedUser)
            .FirstOrDefaultAsync(t => t.Id == dto.TaskItemId && t.TenantId == tenantId && !t.IsDeleted);

        if (task == null)
            throw new KeyNotFoundException("Task not found.");

        // for audit
        var previousAssignedUserId = task.AssignedUserId;

        var user = await _userManager.FindByIdAsync(dto.AssignedUserId);
        if (user == null || user.TenantId != tenantId)
            throw new UnauthorizedAccessException("Invalid user for assignment.");

        // prevent other user except member and special member from getting assigned
        var userRoles = await _userManager.GetRolesAsync(user);
        if (!userRoles.Contains(AppRoles.Member) && !userRoles.Contains(AppRoles.SpecialMember))
        {
            throw new UnauthorizedAccessException("User is not allowed to be assigned to a task.");
        }

        task.AssignedUserId = user.Id;
        task.Status = TaskItemStatus.Assigned; // set the task status assigned once assigned a user

        await _context.SaveChangesAsync();

        // auditlog
        var auditData = new
        {
            // PreviousAssignedUser = task.AssignedUserId,
            PreviousAssignedUser = previousAssignedUserId,
            NewAssignedUser = user.Id,
            NewAssignedUserEmail = user.Email
        };
        
            // differentiating Reassigned and AssignUser actions based on condition
        string actionValue = previousAssignedUserId != null ? "Reassigned" : "AssignUser";

        await _auditService.LogAsync(
            action: actionValue,
            entityName: "TaskItem",
            entityId: task.Id.ToString(),
            changes: JsonSerializer.Serialize(auditData)
        );

        return TaskItemMapper.ToTaskItemDto(task);
        // return task.ToTaskItemDto();
    }

    public async Task<bool> UpdateTaskStatusAsync(int taskId, UpdateTaskItemStatusDto dto)
    {
        var tenantId = _tenantAccessor.TenantId;
        var userId = _userAccessor.UserId;


        var task = await _context.TaskItems.FirstOrDefaultAsync(t => t.Id == taskId && t.TenantId == tenantId);
        
        if (task == null)
            throw new Exception("Task not found.");
        
        // Prevent updates once task is completed
        if (task.Status == TaskItemStatus.Completed)
            throw new InvalidOperationException("Cannot update status. The task has already been completed.");

        // auditlog: original task status snapshot before PATCH  
        var originalTaskStatusDto = task.Status;

        // for custom UpdateTaskItemStatusDtoValidator -------
        var currentStatus = task.Status;
        var validator = new UpdateTaskItemStatusDtoValidator();
        var result = validator.ValidateWithContext(dto, currentStatus);

        if (!result.IsValid)
        {
            throw new InvalidOperationException($"Invalid status update: {string.Join(", ", result.Errors.Select(e => e.ErrorMessage))}");
        }
        // --------

        // since my NewStatus is a string type, Parse the new status from string to enum
        var validStatuses = string.Join(", ", Enum.GetNames(typeof(TaskItemStatus)));
        if (!Enum.TryParse<TaskItemStatus>(dto.NewStatus, out var newStatus))
            throw new InvalidOperationException($"Invalid status value: {dto.NewStatus}. Valid status values are: {validStatuses}.");

        // validate status transition
        if (!TaskStatusTransition.CanTransition(task.Status, newStatus))
            throw new InvalidOperationException($"Cannot transition from {task.Status} to {newStatus}");

        // Set timestamps based on status
        if (task.Status != newStatus)
        {
            if (newStatus == TaskItemStatus.InProgress)
                task.StartedAt = DateTime.UtcNow;
            else if (newStatus == TaskItemStatus.Completed)
                task.CompletedAt = DateTime.UtcNow;
        }

        task.Status = newStatus;
        await _context.SaveChangesAsync();

        // auditlog: refetch after patch
        var updatedTask = await _context.TaskItems.FirstOrDefaultAsync(t => t.Id == taskId && t.TenantId == tenantId);
        var updatedTaskStatusDto = updatedTask!.Status;

        var auditData = new
        {
            Original = originalTaskStatusDto,
            Updated = updatedTaskStatusDto
        };

        string actionValue = dto.NewStatus switch
        {
            nameof(TaskItemStatus.InProgress) => "StartTask",
            nameof(TaskItemStatus.Completed)  => "CompleteTask",
            _ => throw new InvalidOperationException($"Unsupported status: {dto.NewStatus}") // fallback if other statuses are possible
        };

        await _auditService.LogAsync(
            action: actionValue,
            entityName: "TaskItem",
            entityId: task.Id.ToString(),
            changes: JsonSerializer.Serialize(auditData)
        );
        return true;
    
    }

}
    

