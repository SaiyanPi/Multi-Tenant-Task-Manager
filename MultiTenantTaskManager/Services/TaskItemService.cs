using Azure;
using Microsoft.EntityFrameworkCore;
using MultiTenantTaskManager.Accessor;
using MultiTenantTaskManager.Data;
using MultiTenantTaskManager.DTOs.TaskItem;
using MultiTenantTaskManager.Mappers;
using MultiTenantTaskManager.Models;

namespace MultiTenantTaskManager.Services;

public class TaskItemService : ITaskItemService
{
    private readonly ApplicationDbContext _context;
    private readonly ITenantAccessor _tenantAccessor;

    public TaskItemService(ApplicationDbContext context, ITenantAccessor tenantAccessor)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _tenantAccessor = tenantAccessor ?? throw new ArgumentNullException(nameof(tenantAccessor));
    }

    public async Task<IEnumerable<TaskItemDto>> GetAllTaskAsync(int page = 1, int pageSize = 10)
    {
        var tasks = await _context.TaskItems
            .AsNoTracking()
            .Where(t => t.TenantId == _tenantAccessor.TenantId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return tasks.Select(TaskItemMapper.ToTaskItemDto);
    }

    public async Task<TaskItemDto?> GetTaskByIdAsync(int taskId)
    {
        var task = await _context.TaskItems
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == taskId && t.TenantId == _tenantAccessor.TenantId);

        return task == null ? null : TaskItemMapper.ToTaskItemDto(task);
    }


    public async Task<TaskItemDto> CreateTaskAsync(CreateTaskItemDto dto)
    {
        if (dto == null) throw new ArgumentNullException(nameof(dto));

        var tenantId = _tenantAccessor.TenantId;
        bool taskExist = await _context.TaskItems
            .AnyAsync(p => p.TenantId == tenantId && p.Titles == dto.Titles);
        if (taskExist)
        {
            throw new InvalidOperationException($"A task with title '{dto.Titles}' already exists.");
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

        // taskItem.TenantId = _tenantAccessor.TenantId; // <-- Enforce correct TenantId
        // var existingTask = await GetTaskByIdAsync(taskId);
        var existingTask = await _context.TaskItems
            .FirstOrDefaultAsync(t => t.Id == taskId && t.TenantId == _tenantAccessor.TenantId);
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
        // var task = await GetTaskByIdAsync(taskId);
        var task = await _context.TaskItems
            .FirstOrDefaultAsync(t => t.Id == taskId && t.TenantId == _tenantAccessor.TenantId);
        if (task == null) return false;

        if(task.TenantId != _tenantAccessor.TenantId)
        {
            throw new UnauthorizedAccessException("Forbidden: Cross-tenant delete denied");
        }
        
        _context.TaskItems.Remove(task);
        await _context.SaveChangesAsync();

        return true;
    }

}
    

