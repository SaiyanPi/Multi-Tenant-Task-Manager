using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MultiTenantTaskManager.DTOs.AuditLog;
using MultiTenantTaskManager.Services;

namespace MultiTenantTaskManager.Controllers;

[Authorize(Policy = "canManageTenants")]
[ApiController]
[Route("api/[controller]")]
[SkipTenantResolution]
public class LogsController : ControllerBase
{
    private readonly IAuditService _auditService;

    public LogsController(IAuditService auditService)
    {
        _auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));
    }

    // GET:/api/logs
    [HttpGet]
    public async Task<ActionResult<IEnumerable<AuditLogDto>>> GetAllLogs()
    {
        var logs = await _auditService.GetAllAuditLogsAsync();

        return Ok(logs);
    }

    // GET:/api/logs/{id}
    [HttpGet("{logId}")]
    public async Task<ActionResult<IEnumerable<AuditLogDto>>> GetLog(int logId)
    {
        var log = await _auditService.GetAuditLogByIdAsync(logId);

        return Ok(log);
    }
    
    // GET:/api/logs/tenant/{tenantId}
    [HttpGet("tenant/{tenantId}")]
    public async Task<ActionResult<IEnumerable<AuditLogDto>>> GetLogByTenantId(Guid tenantId)
    {
        var tenantLogs = await _auditService.GetAuditLogsByTenantIdAsync(tenantId);

        return Ok(tenantLogs);
    }
}