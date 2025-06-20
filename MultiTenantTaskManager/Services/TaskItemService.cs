using System.Security.Claims;
using System.Text.Json;
using Azure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using MultiTenantTaskManager.Accessor;
using MultiTenantTaskManager.Data;
using MultiTenantTaskManager.DTOs.TaskItem;
using MultiTenantTaskManager.Mappers;
using MultiTenantTaskManager.Models;

namespace MultiTenantTaskManager.Services;

public class TaskItemService : TenantAwareService, ITaskItemService
{
    private readonly ApplicationDbContext _context;
    private readonly IAuditService _auditService;

    public TaskItemService(
        ApplicationDbContext context,
        ClaimsPrincipal user,
        ITenantAccessor tenantAccessor,
        IAuthorizationService authorizationService,
        IAuditService auditService)
        : base(user, tenantAccessor, authorizationService)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));
    }

    public async Task<IEnumerable<TaskItemDto>> GetAllTaskAsync(int page = 1, int pageSize = 10)
    {
        await AuthorizeSameTenantAsync();

        var tenantId = GetCurrentTenantId();

        var tasks = await _context.TaskItems
            .AsNoTracking()
            .Where(t => t.TenantId == tenantId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

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

        return TaskItemMapper.ToTaskItemDto(existingTask);
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

        _context.TaskItems.Remove(task);
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

}
    

