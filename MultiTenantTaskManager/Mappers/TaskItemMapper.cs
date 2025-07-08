using MultiTenantTaskManager.DTOs.TaskItem;
using MultiTenantTaskManager.Helpers;
using MultiTenantTaskManager.Models;

namespace  MultiTenantTaskManager.Mappers;

public static class TaskItemMapper
{
    public static TaskItemDto ToTaskItemDto(this TaskItem task)
    {
        return new TaskItemDto
        {
            Id = task.Id,
            Titles = task.Titles,
            Description = task.Description.Truncate(30),
            ProjectId = task.ProjectId,
            TenantId = task.TenantId,

            // New assignment-related fields
            AssignedUserId = task.AssignedUserId,
            AssignedUserEmail = task.AssignedUser?.Email,
            Status = task.Status,
            DueDate = task.DueDate,
            CreatedAt = task.CreatedAt,
            StartedAt = task.StartedAt,
            CompletedAt = task.CompletedAt

            // soft delete properties
            // IsDeleted = task.IsDeleted,
            // DeletedAt = task.DeletedAt

        };
    }

    public static TaskItem ToTaskItemModel(this CreateTaskItemDto dto)
    {
        if (dto == null) throw new ArgumentNullException(nameof(dto));
        
        return new TaskItem
        {
            Titles = dto.Titles,
            Description = dto.Description,
            ProjectId = dto.ProjectId,
            DueDate = dto.DueDate
        };
    }
    public static void UpdateFromDto(this TaskItem task, UpdateTaskItemDto dto)
    {
        if (dto == null) throw new ArgumentNullException(nameof(dto));

        task.Titles = dto.Titles;
        task.Description = dto.Description;
        task.ProjectId = dto.ProjectId;
        task.DueDate = dto.DueDate;
    }
}
