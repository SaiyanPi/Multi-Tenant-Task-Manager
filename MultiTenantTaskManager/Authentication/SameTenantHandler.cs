using Microsoft.AspNetCore.Authorization;
using MultiTenantTaskManager.Accessor;

namespace MultiTenantTaskManager.Authentication;
public class SameTenantHandler : AuthorizationHandler<SameTenantRequirement>
{
    private readonly ITenantAccessor _tenantAccessor;

    public SameTenantHandler(ITenantAccessor tenantAccessor)
    {
        _tenantAccessor = tenantAccessor;
    }

    // compare the tenant ID from the user’s claims with the tenant ID of the resource.
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, SameTenantRequirement requirement)
    {
        var tenantClaim = context.User.FindFirst("tenant_id")?.Value;
        var currentTenant = _tenantAccessor.TenantId.ToString(); // From header middleware

        if (tenantClaim == currentTenant)
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}