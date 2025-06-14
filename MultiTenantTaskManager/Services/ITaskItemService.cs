using MultiTenantTaskManager.Models;

namespace MultiTenantTaskManager.Services;

public interface ITaskItemService
{
    Task<IEnumerable<TaskItem>> GetAllTaskAsync(int page = 1, int pageSize = 10);
    Task<TaskItem?> GetTaskByIdAsync(int taskId);
    Task<TaskItem> CreateTaskAsync(TaskItem taskItem);
    Task<TaskItem> UpdateTaskAsync(int taskId, TaskItem taskItem);
    Task<bool> DeleteTaskAsync(int tasktId);
}