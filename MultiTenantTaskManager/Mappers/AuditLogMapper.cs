using MultiTenantTaskManager.DTOs.AuditLog;
using MultiTenantTaskManager.DTOs.Project;
using MultiTenantTaskManager.DTOs.Tenant;
using MultiTenantTaskManager.Helpers;
using MultiTenantTaskManager.Models;

namespace  MultiTenantTaskManager.Mappers;

public static class AuditLogMapper
{
    public static AuditLogDto ToLogDto(this AuditLog auditLog)
    {
        return new AuditLogDto
        {
            Id = auditLog.Id,
            Action = auditLog.Action,
            TenantId = auditLog.TenantId,
            UserId = auditLog.UserId,
            UserName = auditLog.UserName,
            EntityName = auditLog.EntityName,
            EntityId = auditLog.EntityId,
            Date = auditLog.Timestamp,
            Changes = auditLog.Changes
        };
    }

}
