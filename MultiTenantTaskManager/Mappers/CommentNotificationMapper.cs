// using MultiTenantTaskManager.DTOs.Notification;
// using MultiTenantTaskManager.Models;

// namespace MultiTenantTaskManager.Mappers;

// public static class CommentNotificationMapper
// {
//     public static CommentNotificationDto ToCommentNotificationDto(this Comment comment)
//     {
//         return new CommentNotificationDto
//         {
//             SenderName = comment.User?.UserName ?? string.Empty, 
//             Content = comment.Content,
//             CreatedAt = comment.CreatedAt,
//             TaskItemId = comment.TaskItemId,
//             ProjectId = comment.ProjectId,
//             TargetUserIds = targetUserIds
//         };
//     }
// }