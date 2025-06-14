using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using MultiTenantTaskManager.Accessor;
using MultiTenantTaskManager.Data;
using MultiTenantTaskManager.Models;

namespace MultiTenantTaskManager.Services;
public class ProjectService : IProjectService
{
    private readonly ApplicationDbContext _context;
    private readonly ITenantAccessor _tenantAccessor;
    private readonly IAuthorizationService _authorizationService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ProjectService(ApplicationDbContext context, ITenantAccessor tenantAccessor,
        IAuthorizationService authorizationService, IHttpContextAccessor httpContextAccessor)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _tenantAccessor = tenantAccessor ?? throw new ArgumentNullException(nameof(tenantAccessor));
        _authorizationService = authorizationService ?? throw new ArgumentNullException(nameof(authorizationService));
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
    }
    private ClaimsPrincipal User
    {
        get
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null || httpContext.User == null)
                throw new InvalidOperationException("HttpContext or User is null.");
            return httpContext.User;
        }
    }


    public async Task<IEnumerable<Project>> GetAllProjectsAsync(int page = 1, int pageSize = 10)
    {
        return await _context.Projects
            .AsNoTracking()
            .Where(p => p.TenantId == _tenantAccessor.TenantId)
            .Include(p => p.Tasks)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<Project?> GetProjectByIdAsync(int projectId)
    {
        var project = await _context.Projects
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == projectId && p.TenantId == _tenantAccessor.TenantId);

        // Run SameTenant policy
        // var authResult = await _authorizationService.AuthorizeAsync(User, null, "SameTenant");
        // if (!authResult.Succeeded) throw new UnauthorizedAccessException("Forbidden: Cross-tenant access denied");
        
        return project;
    }

    public async Task<Project> CreateProjectAsync(Project project)
    {
        if (project == null) throw new ArgumentNullException(nameof(project));

        var tenantId = _tenantAccessor.TenantId;
        bool projectExist = await _context.Projects
            .AnyAsync(p => p.TenantId == tenantId && p.Name == project.Name);
        if (projectExist)
        {
            throw new InvalidOperationException($"A project with name '{project.Name}' already exists.");
        }
        project.TenantId = tenantId; // <-- Enforce correct TenantId
        _context.Projects.Add(project);
        await _context.SaveChangesAsync();

        return project;
    }

    public async Task<Project> UpdateProjectAsync(int projectId, Project project)
    {
        if (project == null) throw new ArgumentNullException(nameof(project));

        project.TenantId = _tenantAccessor.TenantId; // <-- Enforce correct TenantId

        var existingProject = await GetProjectByIdAsync(projectId);
        if (existingProject == null)
        {
            throw new KeyNotFoundException($"Project with ID {projectId} not found.");
        }
        
        _context.Projects.Update(project);
        await _context.SaveChangesAsync();

        return project;
    }

    public async Task<bool> DeleteProjectAsync(int projectId)
    {
        var project = await GetProjectByIdAsync(projectId);
        if (project == null) return false;

        _context.Projects.Remove(project);
        await _context.SaveChangesAsync();

        return true;
    }

}