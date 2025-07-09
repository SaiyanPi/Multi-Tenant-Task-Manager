using MultiTenantTaskManager.DTOs.AuditLog;

namespace MultiTenantTaskManager.Services;

public interface IAuditService
{
    /// <summary>
    /// Logs an audit entry for a specific action performed by a user on an entity.
    /// </summary>
    /// <param name="action">The action performed (e.g., "Create", "Update", "Delete").</param>
    /// <param name="entityName">The name of the entity being acted upon (e.g., "Project", "TaskItem").</param>
    /// <param name="entityId">The ID of the entity being acted upon.</param>
    /// <param name="changes">A JSON or text representation of changes made, if applicable.</param>
    Task LogAsync(string action, string entityName, string entityId, string changes);

    // auditlog endpoint for superadmins
    Task<IEnumerable<AuditLogDto>> GetAllAuditLogsAsync(int page = 1, int pageSize = 20);
    Task<AuditLogDto?> GetAuditLogByIdAsync(int logId);
    Task<IEnumerable<AuditLogDto>> GetAuditLogsByTenantIdAsync(Guid tenantId, int page = 1, int pageSize = 10);

}