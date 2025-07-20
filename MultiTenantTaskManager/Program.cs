using System.Security.Claims;
using System.Text;
using System.Text.Json.Serialization;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using MultiTenantTaskManager.Accessor;
using MultiTenantTaskManager.Authentication;
using MultiTenantTaskManager.Data;
using MultiTenantTaskManager.Hubs;
using MultiTenantTaskManager.Middleware;
using MultiTenantTaskManager.Services;
using MultiTenantTaskManager.Validators;
using SharpGrip.FluentValidation.AutoValidation.Mvc.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services
    .AddControllers()
    // configuring the System.Text.Json framework to ignore the cycle.
    .AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
            options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        });;

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddIdentityCore<ApplicationUser>()
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options =>
{
    var secret = builder.Configuration["JwtConfig:Secret"];
    var issuer = builder.Configuration["JwtConfig:ValidIssuer"];
    var audience = builder.Configuration["JwtConfig:ValidAudiences"];
    if (secret is null || issuer is null || audience is null)
    {
        throw new ApplicationException("Jwt is not set in theconfiguration");
    }
    options.SaveToken = true;
    options.RequireHttpsMetadata = false;
    options.TokenValidationParameters = new TokenValidationParameters()
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidAudience = audience,
        ValidIssuer = issuer,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)),
        // // This is what tells ASP.NET to use ClaimTypes.Name as Identity.Name
        // NameClaimType = ClaimTypes.Name,
        // RoleClaimType = ClaimTypes.Role,
    };

    // KEY piece for SignalR: SignalR sends the token via query string, not the authorization header
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            // If the request is for SignalR and token is in the query string
            var accessToken = context.Request.Query["access_token"];

            // If the request is for the hub...
            var path = context.HttpContext.Request.Path;
            if (!string.IsNullOrEmpty(accessToken) &&
                path.StartsWithSegments("/hubs/notifications"))
            {
                context.Token = accessToken;
            }

            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization(options =>
{
    // TASK POLICIES
    options.AddPolicy("canViewTasks", policy =>
        policy.RequireClaim(AppClaimTypes.can_view_tasks, "true"));

    options.AddPolicy("canManageTasks", policy =>
        policy.RequireClaim(AppClaimTypes.can_manage_tasks, "true"));

    // PROJECT POLICIES
    options.AddPolicy("canManageProjects", policy =>
        policy.RequireClaim(AppClaimTypes.can_manage_projects, "true"));

    // USER MANAGEMENT
    options.AddPolicy("canManageUsers", policy =>
        policy.RequireClaim(AppClaimTypes.can_manage_users, "true"));

    // TENANT MANAGEMENT (for Admin managing their own tenant)
    options.AddPolicy("canManageTenant", policy =>
        policy.RequireClaim(AppClaimTypes.can_manage_tenant, "true"));

    // SUPER ADMIN TENANT MANAGEMENT (all tenants)
    options.AddPolicy("canManageTenants", policy =>
        policy.RequireClaim(AppClaimTypes.can_manage_tenants, "true"));

    // RESOURCE BASED AUTHORIZATION (Tenant-specific policy)
    options.AddPolicy("SameTenant", policy =>
        policy.Requirements.Add(new SameTenantRequirement()));

    options.AddPolicy("AdminWithinTenant", policy =>
    {
        policy.RequireRole("Admin");
        policy.Requirements.Add(new SameTenantRequirement());
    });
});

builder.Services.AddScoped<IAuthorizationHandler, SameTenantHandler>();

builder.Services.AddHttpContextAccessor(); // Needed for IHttpContextAccessor

builder.Services.AddScoped<ClaimsPrincipal>(provider =>
{
    var httpContextAccessor = provider.GetRequiredService<IHttpContextAccessor>();
    var user = httpContextAccessor.HttpContext?.User;

    return user ?? new ClaimsPrincipal(new ClaimsIdentity());
});

// builder.Services.AddScoped<ClaimsPrincipal>(sp =>
//     sp.GetRequiredService<IHttpContextAccessor>().HttpContext?.User ?? new ClaimsPrincipal());

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddScoped<ITenantAccessor, TenantAccessor>();
builder.Services.AddScoped<ITenantService, TenantService>();
builder.Services.AddScoped<IProjectService, ProjectService>();
builder.Services.AddScoped<ITaskItemService, TaskItemService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<IUserAccessor, UserAccessor>();
builder.Services.AddScoped<IUserValidator<ApplicationUser>, MultiTenantUserValidator>();

// Fluent validation = validation logic that is independent of business logic that is why we don't mention it inside service classes
builder.Services
    // .AddFluentValidationAutoValidation(); // Enables automatic model validation
    // .AddFluentValidationClientsideAdapters(); // Enables client-side adapters (optional, useful for Razor or Blazor apps)
    .AddFluentValidationAutoValidation(options =>
    {
        options.DisableBuiltInModelValidation = true; // Allows async rules
    });
// Register all validators from the current assembly or specify explicitly
builder.Services.AddValidatorsFromAssemblyContaining<CreateProjectDtoValidator>();

builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddSingleton<IUserIdProvider, CustomUserIdProvider>();
builder.Services.AddScoped<ICommentService, CommentService>();



// Add SignalR
builder.Services.AddSignalR();
// Enable CORS
var corsPolicy = new CorsPolicyBuilder()
    .AllowAnyHeader()
    .AllowAnyMethod()
    .AllowCredentials()
    .WithOrigins("http://127.0.0.1:5500")
    .Build();
builder.Services.AddCors(options =>{
    options.AddPolicy("CorsPolicy", corsPolicy);
});



var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("CorsPolicy");

app.UseRouting();

app.UseAuthentication();

app.UseAuthorization();

// registering custom middleware for tenant resolution
app.UseMiddleware<TenantResolutionMiddleware>();


// check if the roles exist in database table AspNetRoles, if no then create them as follows.
using (var serviceScope = app.Services.CreateScope())
{
    var services = serviceScope.ServiceProvider;

    // Ensure the database is created.
    var dbContext = services.GetRequiredService<ApplicationDbContext>();
    dbContext.Database.EnsureCreated();

    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
    if (!await roleManager.RoleExistsAsync(AppRoles.SuperAdmin))
    {
        await roleManager.CreateAsync(new IdentityRole(AppRoles.SuperAdmin));
    }
    if (!await roleManager.RoleExistsAsync(AppRoles.Admin))
    {
        await roleManager.CreateAsync(new IdentityRole(AppRoles.Admin));
    }
    if (!await roleManager.RoleExistsAsync(AppRoles.Manager))
    {
        await roleManager.CreateAsync(new IdentityRole(AppRoles.Manager));
    }
    if (!await roleManager.RoleExistsAsync(AppRoles.SpecialMember))
    {
        await roleManager.CreateAsync(new IdentityRole(AppRoles.SpecialMember));
    }
    if (!await roleManager.RoleExistsAsync(AppRoles.Member))
    {
        await roleManager.CreateAsync(new IdentityRole(AppRoles.Member));
    }
}

app.MapControllers();

app.MapHub<NotificationHub>(NotificationHub.HubUrl);

app.Run();
