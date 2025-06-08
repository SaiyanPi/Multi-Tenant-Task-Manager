using MultiTenantTaskManager.Models;

namespace MultiTenantTaskManager.Services;

public interface ITaskItemService
{
    Task<IEnumerable<TaskItem>> GetAllTaskAsync();
    Task<TaskItem?> GetTaskByIdAsync(int taskId);
    Task<TaskItem> CreateTaskAsync(TaskItem taskItem);
    Task<TaskItem> UpdateTaskAsync(int taskId, TaskItem taskItem);
    Task<bool> DeleteTaskAsync(int tasktId);
}