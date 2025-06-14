using System.Text;
using System.Text.Json;
using Azure.Core;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MultiTenantTaskManager.Accessor;
using MultiTenantTaskManager.Authentication;
using MultiTenantTaskManager.Authentication.DTOs;
using MultiTenantTaskManager.Data;

namespace MultiTenantTaskManager.Middleware;

public class TenantResolutionMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, ITenantAccessor tenantAccessor,
    ApplicationDbContext dbContext, UserManager<ApplicationUser> userManager)
    {
        // If the request is for tenant management endpoints, skip tenant resolution
        // This is useful for endpoints like tenant creation or management that do not require a tenant context
    
        // // following code is commented because it is not feasible to Hardcode Middleware Exceptions for 
        // // Every Tenant Endpoint

        // // // Allow open endpoints like tenant creation
        // // var path = context.Request.Path.Value?.ToLower();
        // // if (path != null && path.StartsWith("/api/tenant") && context.Request.Method == "POST")
        // // {
        // //     // Skip tenant resolution for tenant management endpoints
        // //     await next(context);
        // //     return;
        // // }

        // // Check if the endpoint has the SkipTenantResolution attribute
        // var endpoint = context.GetEndpoint();
        // var skipTenantResolution = endpoint?.Metadata.GetMetadata<SkipTenantResolutionAttribute>() != null;
        // if (skipTenantResolution)
        // {
        //     await next(context);
        //     return;
        // }

        // // if (context.Request.Headers.TryGetValue("X-Tenant-ID", out var tenantIdHeader) &&
        // //     Guid.TryParse(tenantIdHeader, out var tenantId))
        // // {
        // //     tenantAccessor.TenantId = tenantId;
        // // }
        // // else
        // // {
        // //     // You could return 400 here or fallback logic
        // //     context.Response.StatusCode = 400;
        // //     await context.Response.WriteAsync("Tenant ID is required in X-Tenant-ID header.");
        // //     return;
        // // }
        // // await next(context);

        // if (!context.Request.Headers.TryGetValue("X-Tenant-ID", out var tenantIdHeader) ||
        //     !Guid.TryParse(tenantIdHeader, out var tenantId))
        // {
        //     context.Response.StatusCode = 400;
        //     await context.Response.WriteAsync("Missing or invalid X-Tenant-ID header.");
        //     return;
        // }

        // // Check if tenant exists in the database
        // var tenantExists = await dbContext.Tenants.AnyAsync(t => t.Id == tenantId);
        // if (!tenantExists)
        // {
        //     context.Response.StatusCode = 400;
        //     await context.Response.WriteAsync("Invalid Tenant ID.");
        //     return;
        // }

        // tenantAccessor.TenantId = tenantId;

        // await next(context);

    // ---------------------------------------------------------------------------------------------------

        // 1. Skip if marked with [SkipTenantResolution]
        var endpoint = context.GetEndpoint();
        var skipTenantResolution = endpoint?.Metadata.GetMetadata<SkipTenantResolutionAttribute>() != null;
        if (skipTenantResolution)
        {
            await next(context);
            return;
        }

        // following code is commented because when logging in user as SuperAdmin, user is not authenticated
        // yet so we cannot skip the middleware at the time of logging so these code lines does not hit
        // If the user is authenticated and is SuperAdmin → skip tenant enforcement
        // var user = context.User;
        // if (user?.Identity?.IsAuthenticated == true && user.IsInRole("SuperAdmin"))
        // {
        //     await next(context);
        //     return;
        // }

        // 5. Skip Tenant Resolution on Login IF the Email Belongs to a SuperAdmin
        var isLoginEndpoint = context.Request.Path.Equals("/api/account/login", StringComparison.OrdinalIgnoreCase);

        if (isLoginEndpoint && context.Request.Method == HttpMethods.Post)
        {
            // Read body for email (requires buffering)
            context.Request.EnableBuffering();

            using var reader = new StreamReader(
                context.Request.Body,
                encoding: Encoding.UTF8,
                detectEncodingFromByteOrderMarks: false,
                bufferSize: 1024,
                leaveOpen: true);

            var body = await reader.ReadToEndAsync();
            context.Request.Body.Position = 0;

            var loginData = JsonSerializer.Deserialize<LoginDto>(body, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            
            if (loginData?.Email != null)
            {
                var user = await userManager.FindByEmailAsync(loginData.Email);
                if (user != null)
                {
                    var roles = await userManager.GetRolesAsync(user);
                    if (roles.Contains(AppRoles.SuperAdmin))
                    {
                        // Skip tenant resolution only for SuperAdmin login
                        await next(context);
                        return;
                    }
                }
            }
        }

        Guid tenantId;

        // 2. Try to extract tenant from JWT claims
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var tenantClaim = context.User.FindFirst("tenant_id");
            if (tenantClaim != null && Guid.TryParse(tenantClaim.Value, out tenantId))
            {
                // Validate tenant exists in DB
                var tenantExists = await dbContext.Tenants.AnyAsync(t => t.Id == tenantId);
                if (!tenantExists)
                {
                    context.Response.StatusCode = 400;
                    await context.Response.WriteAsync("Invalid Tenant ID in token.");
                    return;
                }

                tenantAccessor.TenantId = tenantId;
                await next(context);
                return;
            }
        }

        // 3. Fallback to X-Tenant-ID header (optional)
        if (context.Request.Headers.TryGetValue("X-Tenant-ID", out var tenantIdHeader) &&
            Guid.TryParse(tenantIdHeader, out tenantId))
        {
            var tenantExists = await dbContext.Tenants.AnyAsync(t => t.Id == tenantId);
            if (!tenantExists)
            {
                context.Response.StatusCode = 400;
                await context.Response.WriteAsync("Invalid Tenant ID in header.");
                return;
            }

            tenantAccessor.TenantId = tenantId;
            await next(context);
            return;
        }

        // 4. Fail if no valid tenant ID found
        context.Response.StatusCode = 400;
        await context.Response.WriteAsync("Missing or invalid tenant identifier.");
    }
    
}