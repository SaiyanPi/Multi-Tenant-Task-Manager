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
        // following code is commented because it is not feasible to Hardcode Middleware Exceptions for 
        // Every Tenant Endpoint

        // // Allow open endpoints like tenant creation
        // var path = context.Request.Path.Value?.ToLower();
        // if (path != null && path.StartsWith("/api/tenant") && context.Request.Method == "POST")
        // {
        //     // Skip tenant resolution for tenant management endpoints
        //     await next(context);
        //     return;
        // }

        // Check if the endpoint has the SkipTenantResolution attribute
        var endpoint = context.GetEndpoint();
        var skipTenantResolution = endpoint?.Metadata.GetMetadata<SkipTenantResolutionAttribute>() != null;
        if (skipTenantResolution)
        {
            await next(context);
            return;
        }

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