using System.Security.Claims;
using MultiTenantTaskManager.Accessor;

namespace MultiTenantTaskManager.Services;

public class UserAccessor : IUserAccessor
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public UserAccessor(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

    public Guid? UserId => 
        User?.FindFirst(ClaimTypes.NameIdentifier)?.Value is string id && Guid.TryParse(id, out var guid)
            ? guid
            : null;

    public string? UserName => User?.FindFirst(ClaimTypes.Name)?.Value;

    public string? Email => User?.FindFirst(ClaimTypes.Email)?.Value;
    
    public Guid? TenantId =>
        User?.FindFirst("tenant_id")?.Value is string tenantId && Guid.TryParse(tenantId, out var guid)
            ? guid
            : null;

    public bool IsAuthenticated => User?.Identity?.IsAuthenticated == true;

    public bool IsInRole(string role) => User?.IsInRole(role) == true;
}