using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using MultiTenantTaskManager.Accessor;
using MultiTenantTaskManager.Authentication;
using MultiTenantTaskManager.Data;
using MultiTenantTaskManager.DTOs.AuditLog;
using MultiTenantTaskManager.Mappers;
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

        // check if the user is a SuperAdmin, if yes then assign the TenantId to null because
        // SuperAdmin lives outside Tenant scope
        var userRole = user?.Claims
            .FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;
        Console.WriteLine($"User Role: {userRole}");
        if (userRole == AppRoles.SuperAdmin)
        {
            var superAdminAudit = new AuditLog
            {
                UserId = user?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty,
                UserName = user?.Identity?.Name ?? string.Empty,
                Action = action,
                EntityName = entityName,
                EntityId = entityId,
                Changes = changes,
                TenantId = null // assign null to TenantId for SuperAdmin
            };

            _dbContext.AuditLogs.Add(superAdminAudit);
            await _dbContext.SaveChangesAsync();
            return; // end the execution
        }

        // For regular users, we need to get the TenantId from the TenantAccessor
        var tenantId = _tenantAccessor.TenantId;

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

    // auditlog endpoint for superadmins
    public async Task<IEnumerable<AuditLogDto>> GetAllAuditLogsAsync(int page = 1, int pageSize = 20)
    {
        var auditLogs = await _dbContext.AuditLogs
            .AsNoTracking()
            .OrderBy(l => l.EntityName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return auditLogs.Select(AuditLogMapper.ToLogDto);

    }

    // public async Task<AuditLogDto?> GetAuditLogByIdAsync(int logId)
    // {
    //     var auditLog = await _dbContext.AuditLogs
    //     .AsNoTracking()
    //     .FirstOrDefaultAsync(l => l.Id )
    // }
    
}