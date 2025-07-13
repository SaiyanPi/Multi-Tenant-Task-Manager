using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using MultiTenantTaskManager.Accessor;

namespace MultiTenantTaskManager.Services;

// TenantAwareService uses tenant accessor
public abstract class TenantAwareService
{
    protected readonly ClaimsPrincipal _user;
    protected readonly ITenantAccessor _tenantAccessor;
    protected readonly IAuthorizationService _authorizationService;

    protected TenantAwareService(ClaimsPrincipal user, ITenantAccessor tenantAccessor,
        IAuthorizationService authorizationService)
    {
        _user = user ?? throw new ArgumentNullException(nameof(user));
        _tenantAccessor = tenantAccessor ?? throw new ArgumentNullException(nameof(tenantAccessor));
        _authorizationService = authorizationService ?? throw new ArgumentNullException(nameof(authorizationService));
    }

    protected async Task AuthorizeSameTenantAsync()
    {
        var authResult = await _authorizationService.AuthorizeAsync(_user, null, "SameTenant");
        if (!authResult.Succeeded)
        {
            throw new UnauthorizedAccessException("Forbidden: Cross-tenant access denied");
        }
    }

    protected Guid GetCurrentTenantId() => _tenantAccessor.TenantId;

}