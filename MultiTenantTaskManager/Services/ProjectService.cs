using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using MultiTenantTaskManager.Accessor;
using MultiTenantTaskManager.Data;
using MultiTenantTaskManager.DTOs;
using MultiTenantTaskManager.DTOs.Project;
using MultiTenantTaskManager.Mappers;
using MultiTenantTaskManager.Models;

namespace MultiTenantTaskManager.Services;
public class ProjectService : TenantAwareService, IProjectService
{
    private readonly ApplicationDbContext _context;
    // private readonly ITenantAccessor _tenantAccessor;
    // private readonly IAuthorizationService _authorizationService;
    // private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IAuditService _auditService;
    private readonly IUserAccessor _userAccessor;

    public ProjectService(
        ApplicationDbContext context,
        ClaimsPrincipal user,
        ITenantAccessor tenantAccessor,
        IAuthorizationService authorizationService,
        IAuditService auditService,
        IUserAccessor userAccessor)
        : base(user, tenantAccessor, authorizationService)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));
        // _tenantAccessor = tenantAccessor ?? throw new ArgumentNullException(nameof(tenantAccessor));
        // _authorizationService = authorizationService ?? throw new ArgumentNullException(nameof(authorizationService));
        // _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        _userAccessor = userAccessor ?? throw new ArgumentNullException(nameof(userAccessor));
    }   
    // Uncomment if you need to access User directly
    // private ClaimsPrincipal User
    // {
    //     get
    //     {
    //         var httpContext = _httpContextAccessor.HttpContext;
    //         if (httpContext == null || httpContext.User == null)
    //             throw new InvalidOperationException("HttpContext or User is null.");
    //         return httpContext.User;
    //     }
    // }


    public async Task<IEnumerable<ProjectDto>> GetAllProjectsAsync(int page = 1, int pageSize = 10)
    {
        // // Run SameTenant policy (against current user and current tenant)
        // var authResult = await _authorizationService.AuthorizeAsync(User, null, "SameTenant");
        // if (!authResult.Succeeded) throw new UnauthorizedAccessException("Forbidden: Cross-tenant access denied");

        await AuthorizeSameTenantAsync();

        var tenantId = GetCurrentTenantId();

        var projects = await _context.Projects
            .AsNoTracking()
            // .Where(p => p.TenantId == _tenantAccessor.TenantId)
            .Where(p => p.TenantId == tenantId)
            .OrderBy(p => p.Name)
            .Include(p => p.Tasks)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return projects.Select(ProjectMapper.ToProjectDto);

    }

    public async Task<ProjectDto?> GetProjectByIdAsync(int projectId)
    {
        // // Run SameTenant policy
        // var authResult = await _authorizationService.AuthorizeAsync(User, null, "SameTenant");
        // if (!authResult.Succeeded) throw new UnauthorizedAccessException("Forbidden: Cross-tenant access denied");

        await AuthorizeSameTenantAsync();

        var tenantId = GetCurrentTenantId();

        var project = await _context.Projects
            .AsNoTracking()
            // .FirstOrDefaultAsync(p => p.Id == projectId && p.TenantId == _tenantAccessor.TenantId);
            .FirstOrDefaultAsync(p => p.Id == projectId && p.TenantId == tenantId);
            
        return project == null ? null : ProjectMapper.ToProjectDto(project);
    }

    public async Task<ProjectDto> CreateProjectAsync(CreateProjectDto dto)
    {
        if (dto == null) throw new ArgumentNullException(nameof(dto));


        // var authResult = await _authorizationService.AuthorizeAsync(User, null, "SameTenant");
        // if (!authResult.Succeeded) throw new UnauthorizedAccessException("Forbidden: Cross-tenant access denied");

        await AuthorizeSameTenantAsync();

        var tenantId = GetCurrentTenantId();

        bool exists = await _context.Projects
            .AnyAsync(p => p.TenantId == tenantId && p.Name == dto.Name);

        if (exists)
            throw new InvalidOperationException($"Project '{dto.Name}' already exists.");

        var project = dto.ToProjectModel();
        // project.TenantId = _tenantAccessor.TenantId;

        project.TenantId = tenantId;

        _context.Projects.Add(project);
        await _context.SaveChangesAsync();

        // AuditLog
        await _auditService.LogAsync(
            action: "Create",
            entityName: "Project",
            entityId: project.Id.ToString(),
            changes: JsonSerializer.Serialize(ProjectMapper.ToProjectDto(project))
        );

        return ProjectMapper.ToProjectDto(project);
    }

    public async Task<ProjectDto> UpdateProjectAsync(int projectId, UpdateProjectDto dto)
    {
        if (dto == null) throw new ArgumentNullException(nameof(dto));

        await AuthorizeSameTenantAsync();

        var tenantId = GetCurrentTenantId();

        // // Run SameTenant policy before mutations
        // var authResult = await _authorizationService.AuthorizeAsync(User, null, "SameTenant");
        // if (!authResult.Succeeded) throw new UnauthorizedAccessException("Forbidden: Cross-tenant access denied");

        // var existingProject = await GetProjectByIdAsync(projectId);
        var existingProject = await _context.Projects
            // .FirstOrDefaultAsync(p => p.Id == projectId && p.TenantId == _tenantAccessor.TenantId);
            .FirstOrDefaultAsync(p => p.Id == projectId && p.TenantId == tenantId);

        if (existingProject == null)
        {
            throw new KeyNotFoundException($"Project {dto.Id} not found.");
        }

        // original project DTO snapshot before update
        var originalProjectDto = ProjectMapper.ToProjectDto(existingProject);

        // Now retrieve it again for tracking changes
        var trackedProject = await _context.Projects
            .FirstAsync(p => p.Id == projectId && p.TenantId == tenantId);

        trackedProject.UpdateFromDto(dto);

        await _context.SaveChangesAsync();

        // project DTO after update
        var updatedProjectDto = ProjectMapper.ToProjectDto(trackedProject);

        // Log both old and new state
        var auditData = new
        {
            Original = originalProjectDto,
            Updated = updatedProjectDto
        };

        await _auditService.LogAsync(
            action: "Update",
            entityName: "Project",
            entityId: existingProject.Id.ToString(),
            changes: JsonSerializer.Serialize(auditData)
        );

        return ProjectMapper.ToProjectDto(existingProject);
    }


    public async Task<bool> DeleteProjectAsync(int projectId)
    {
        // // Run SameTenant policy
        // var authResult = await _authorizationService.AuthorizeAsync(User, null, "SameTenant");
        // if (!authResult.Succeeded) throw new UnauthorizedAccessException("Forbidden: Cross-tenant access denied");
        
        await AuthorizeSameTenantAsync();

        var tenantId = GetCurrentTenantId();

        // var project = await GetProjectByIdAsync(projectId);
        var project = await _context.Projects
            // .FirstOrDefaultAsync(p => p.Id == projectId && p.TenantId == _tenantAccessor.TenantId);
            .FirstOrDefaultAsync(p => p.Id == projectId && p.TenantId == tenantId);

        if (project == null) return false;

        // project DTO before deletion
        var deletedProjectDto = ProjectMapper.ToProjectDto(project);

        // _context.Projects.Remove(project);

        // Soft delete
        project.IsDeleted = true;
        project.DeletedAt = DateTime.UtcNow;
        project.DeletedBy = _userAccessor.UserName ?? "Unknown";

        await _context.SaveChangesAsync();

        // audit Log the after deletion
        await _auditService.LogAsync(
            action: "Delete",
            entityName: "Project",
            entityId: project.Id.ToString(),
            changes: JsonSerializer.Serialize(deletedProjectDto)
        );

        return true;
    }

}