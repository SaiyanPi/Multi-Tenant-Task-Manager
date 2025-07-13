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
        // following code is commented because we extract tenantId from JWT token in step 2
        
        // // 0. Handle SignalR requests with tenantId in query string from NotificationClient
        // var path = context.Request.Path.Value;
        // if (path != null && path.StartsWith("/hubs/notifications", StringComparison.OrdinalIgnoreCase))
        // {
        //     var tenantIdQuery = context.Request.Query["tenantId"].FirstOrDefault();

        //     if (!string.IsNullOrWhiteSpace(tenantIdQuery) && Guid.TryParse(tenantIdQuery, out var tenantGuid))
        //     {
        //         var tenantExists = await dbContext.Tenants.AnyAsync(t => t.Id == tenantGuid);
        //         if (!tenantExists)
        //         {
        //             context.Response.StatusCode = 400;
        //             await context.Response.WriteAsync("Invalid tenant identifier in query string.");
        //             return;
        //         }

        //         tenantAccessor.TenantId = tenantGuid;
        //         await next(context);
        //         return;
        //     }

        //     // Reject if missing or invalid
        //     context.Response.StatusCode = 400;
        //     await context.Response.WriteAsync("Missing or invalid tenant identifier in query string for SignalR.");
        //     return;
        // }



        // 1. Skip if marked with [SkipTenantResolution]
        var endpoint = context.GetEndpoint();
        var skipTenantResolution = endpoint?.Metadata.GetMetadata<SkipTenantResolutionAttribute>() != null;
        if (skipTenantResolution)
        {
            await next(context);
            return;
        }

        // 0.0. Skip for SignalR negotiate requests (no token yet)
        // if we remove following code, the connection will still be made and the notification will still be
        // pushed but there will be some errors in the dev console regarding WebSocket. The error occurs because
        // TenantResolutionMiddleware is rejecting unauthenticated or unauthenticated WebSocket preflight
        // requests, before SignalR can even negotiate the connection.
        
        if (context.Request.Path.StartsWithSegments("/hubs/notifications", StringComparison.OrdinalIgnoreCase)
            && context.Request.Query.ContainsKey("id")) // this means it's the negotiation or WebSocket upgrade
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

        // 2. Skip Tenant Resolution on Login IF the Email Belongs to a SuperAdmin
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
                // var user = await userManager.FindByEmailAsync(loginData.Email);

                // The default ASP.NET Identity UserManager assumes emails are globally unique. But this is
                // a multi-tenant app therefore, multiple users with the same email but different.
                // Instead, we need to query the users filtered by email AND tenant ID.

                var user = await userManager.Users
                    .Where(u => u.Email == loginData.Email && u.TenantId == null)
                    .SingleOrDefaultAsync();

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

        // 3. Try to extract tenant from JWT claims
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

        // 4. Fallback to X-Tenant-ID header (optional)
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

        // 5. Fail if no valid tenant ID found
        context.Response.StatusCode = 400;
        await context.Response.WriteAsync("Missing or invalid tenant identifier.");

        // ---------------------------------------------------------------------------------------------------

    }
    
}