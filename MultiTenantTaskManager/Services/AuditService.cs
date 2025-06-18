using System.Security.Claims;
using MultiTenantTaskManager.Accessor;
using MultiTenantTaskManager.Data;
using MultiTenantTaskManager.Models;

namespace MultiTenantTaskManager.Services;

public class AuditService : IAuditService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ITenantAccessor _tenantAccessor;

    public AuditService(ApplicationDbContext dbContext, IHttpContextAccessor httpContextAccessor,
        ITenantAccessor tenantAccessor)
    {
        _dbContext = dbContext;
        _httpContextAccessor = httpContextAccessor;
        _tenantAccessor = tenantAccessor;
    }
    public async Task LogAsync(string action, string entityName, string entityId, string changes)
    {
        var user = _httpContextAccessor.HttpContext?.User;
        var tenantId = _tenantAccessor.TenantId;

        if (user == null || tenantId == Guid.Empty)
        {
            return; // No user or tenant context available
        }

        var audit = new AuditLog
        {
            UserId = user?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty,
            UserName = user?.Identity?.Name ?? string.Empty,
            Action = action,
            EntityName = entityName,
            EntityId = entityId,
            Changes = changes,
            TenantId = tenantId
        };

        _dbContext.AuditLogs.Add(audit);
        await _dbContext.SaveChangesAsync();
    }
}