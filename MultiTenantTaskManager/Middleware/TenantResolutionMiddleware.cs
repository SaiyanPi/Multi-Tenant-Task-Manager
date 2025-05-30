using Azure.Core;
using Microsoft.EntityFrameworkCore;
using MultiTenantTaskManager.Accessor;
using MultiTenantTaskManager.Data;

namespace MultiTenantTaskManager.Middleware;

public class TenantResolutionMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, ITenantAccessor tenantAccessor,
    ApplicationDbContext dbContext)
    {
        // if (context.Request.Headers.TryGetValue("X-Tenant-ID", out var tenantIdHeader) &&
        //     Guid.TryParse(tenantIdHeader, out var tenantId))
        // {
        //     tenantAccessor.TenantId = tenantId;
        // }
        // else
        // {
        //     // You could return 400 here or fallback logic
        //     context.Response.StatusCode = 400;
        //     await context.Response.WriteAsync("Tenant ID is required in X-Tenant-ID header.");
        //     return;
        // }
        // await next(context);

        if (!context.Request.Headers.TryGetValue("X-Tenant-ID", out var tenantIdHeader) ||
            !Guid.TryParse(tenantIdHeader, out var tenantId))
        {
            context.Response.StatusCode = 400;
            await context.Response.WriteAsync("Missing or invalid X-Tenant-ID header.");
            return;
        }
        
        // Check if tenant exists in the database
        var tenantExists = await dbContext.Tenants.AnyAsync(t => t.Id == tenantId);
        if (!tenantExists)
        {
            context.Response.StatusCode = 400;
            await context.Response.WriteAsync("Invalid Tenant ID.");
            return;
        }

        tenantAccessor.TenantId = tenantId;

        await next(context);
    }
}