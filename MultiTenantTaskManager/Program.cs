using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using MultiTenantTaskManager.Accessor;
using MultiTenantTaskManager.Authentication;
using MultiTenantTaskManager.Data;
using MultiTenantTaskManager.Middleware;
using MultiTenantTaskManager.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services
    .AddControllers()
    // configuring the System.Text.Json framework to ignore the cycle.
    .AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
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
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret))
    };
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("CanCreateDeleteTenant", policy =>
        policy.RequireClaim(AppClaimTypes.can_create_delete_tenant, "true"));

    options.AddPolicy("CanCreateDeleteProject", policy =>
        policy.RequireClaim(AppClaimTypes.can_create_delete_project, "true"));

    options.AddPolicy("CanCreateDeleteTask", policy =>
        policy.RequireClaim(AppClaimTypes.can_create_delete_task, "true"));

    options.AddPolicy("CanManageTasks", policy =>
        policy.RequireClaim(AppClaimTypes.can_create_delete_task, "true"));

    // since everyone can view tasks, we could make it anonymous without a policy
    // options.AddPolicy("CanViewTasks", policy =>
    //     policy.RequireClaim(AppClaimTypes.can_create_delete_task, "false"));
});


builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddScoped<ITenantAccessor, TenantAccessor>();
builder.Services.AddScoped<ITenantService, TenantService>();
builder.Services.AddScoped<IProjectService, ProjectService>();
builder.Services.AddScoped<ITaskItemService, TaskItemService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// registering custom middleware for tenant resolution
app.UseMiddleware<TenantResolutionMiddleware>();

app.UseAuthentication();

app.UseAuthorization();

// check if the roles exist in database table AspNetRoles, if no then create them as follows.
using (var serviceScope = app.Services.CreateScope())
{
    var services = serviceScope.ServiceProvider;

    // Ensure the database is created.
    var dbContext = services.GetRequiredService<ApplicationDbContext>();
    dbContext.Database.EnsureCreated();

    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
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

app.Run();
