using MultiTenantTaskManager.DTOs;
using MultiTenantTaskManager.DTOs.TaskItem;
using MultiTenantTaskManager.Models;

namespace MultiTenantTaskManager.Services;

public interface ITaskItemService
{
    Task<IEnumerable<TaskItemDto>> GetAllTaskAsync(int page = 1, int pageSize = 10);
    Task<TaskItemDto?> GetTaskByIdAsync(int taskId);
    Task<TaskItemDto> CreateTaskAsync(CreateTaskItemDto dto);
    Task<TaskItemDto> UpdateTaskAsync(int taskId, UpdateTaskItemDto dto);
    Task<bool> DeleteTaskAsync(int taskId);

    Task<TaskItemDto> AssignTaskAsync(AssignTaskDto dto);
}