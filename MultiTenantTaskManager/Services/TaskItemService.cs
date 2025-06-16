using System.Security.Claims;
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

    public TaskItemService(
        ApplicationDbContext context,
        ClaimsPrincipal user,
        ITenantAccessor tenantAccessor,
        IAuthorizationService authorizationService)
        : base(user, tenantAccessor, authorizationService)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
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
            .FirstOrDefaultAsync(t => t.Id == taskId && t.TenantId == tenantId);
        if (existingTask == null)
        {
            throw new KeyNotFoundException($"Task with ID {dto.Id} not found.");
        }

        existingTask.UpdateFromDto(dto);
        await _context.SaveChangesAsync();

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

        if(task.TenantId != tenantId)
        {
            throw new UnauthorizedAccessException("Forbidden: Cross-tenant delete denied");
        }
        
        _context.TaskItems.Remove(task);
        await _context.SaveChangesAsync();

        return true;
    }

}
    

