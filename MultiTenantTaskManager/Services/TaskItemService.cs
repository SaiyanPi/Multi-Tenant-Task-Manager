using Azure;
using Microsoft.EntityFrameworkCore;
using MultiTenantTaskManager.Accessor;
using MultiTenantTaskManager.Data;
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

    public async Task<IEnumerable<TaskItem>> GetAllTaskAsync(int page = 1, int pageSize = 10)
    {
        return await _context.TaskItems
            .AsNoTracking()
            .Where(t => t.TenantId == _tenantAccessor.TenantId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<TaskItem?> GetTaskByIdAsync(int taskId)
    {
        return await _context.TaskItems
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == taskId && t.TenantId == _tenantAccessor.TenantId);
    }


    public async Task<TaskItem> CreateTaskAsync(TaskItem taskItem)
    {
        if (taskItem == null) throw new ArgumentNullException(nameof(taskItem));

        var tenantId = _tenantAccessor.TenantId;
        bool taskExist = await _context.TaskItems
            .AnyAsync(p => p.TenantId == tenantId && p.Titles == taskItem.Titles);
        if (taskExist)
        {
            throw new InvalidOperationException($"A task with title '{taskItem.Titles}' already exists.");
        }
        taskItem.TenantId = tenantId; // <-- Enforce correct TenantId

        _context.TaskItems.Add(taskItem);
        await _context.SaveChangesAsync();

        return taskItem;
    }

    public async Task<TaskItem> UpdateTaskAsync(int taskId, TaskItem taskItem)
    {
        if (taskItem == null) throw new ArgumentNullException(nameof(taskItem));

        taskItem.TenantId = _tenantAccessor.TenantId; // <-- Enforce correct TenantId

        var existingTask = await GetTaskByIdAsync(taskId);
        if (existingTask == null)
        {
            throw new KeyNotFoundException($"Task with ID {taskId} not found.");
        }
        
        _context.TaskItems.Update(taskItem);
        await _context.SaveChangesAsync();

        return taskItem;
    }

    public async Task<bool> DeleteTaskAsync(int taskId)
    {
        var task = await GetTaskByIdAsync(taskId);
        if (task == null) return false;

        _context.TaskItems.Remove(task);
        await _context.SaveChangesAsync();

        return true;
    }

}
    

