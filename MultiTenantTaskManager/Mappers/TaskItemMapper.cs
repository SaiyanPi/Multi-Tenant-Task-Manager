using MultiTenantTaskManager.DTOs.TaskItem;
using MultiTenantTaskManager.Models;

namespace  MultiTenantTaskManager.Mappers;

public static class TaskItemMapper
{
    public static TaskItemDto ToTaskItemDto(this TaskItem entity)
    {
        return new TaskItemDto
        {
            Id = entity.Id,
            Titles = entity.Titles,
            ProjectId = entity.ProjectId,
            TenantId = entity.TenantId,

             // New assignment-related fields
            AssignedUserId = entity.AssignedUserId,
            AssignedUserEmail = entity.AssignedUser?.Email,

            // soft delete properties
            IsDeleted = entity.IsDeleted,
            DeletedAt = entity.DeletedAt
            
        };
    }

    public static TaskItem ToTaskItemModel(this CreateTaskItemDto dto)
    {
        if (dto == null) throw new ArgumentNullException(nameof(dto));
        
        return new TaskItem
        {
            Titles = dto.Titles,
            ProjectId = dto.ProjectId
        };
    }
    public static void UpdateFromDto(this TaskItem task, UpdateTaskItemDto dto)
    {
        if (dto == null) throw new ArgumentNullException(nameof(dto));

        task.Titles = dto.Titles;
        task.ProjectId = dto.ProjectId;
    }
}
