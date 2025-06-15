using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using MultiTenantTaskManager.Accessor;
using MultiTenantTaskManager.Data;
using MultiTenantTaskManager.DTOs;
using MultiTenantTaskManager.DTOs.Project;
using MultiTenantTaskManager.Mappers;
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


    public async Task<IEnumerable<ProjectDto>> GetAllProjectsAsync(int page = 1, int pageSize = 10)
    {
        var projects = await _context.Projects
            .AsNoTracking()
            .Where(p => p.TenantId == _tenantAccessor.TenantId)
            .Include(p => p.Tasks)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return projects.Select(ProjectMapper.ToProjectDto);

    }

    public async Task<ProjectDto?> GetProjectByIdAsync(int projectId)
    {
        var project = await _context.Projects
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == projectId && p.TenantId == _tenantAccessor.TenantId);

        // Run SameTenant policy
        // var authResult = await _authorizationService.AuthorizeAsync(User, null, "SameTenant");
        // if (!authResult.Succeeded) throw new UnauthorizedAccessException("Forbidden: Cross-tenant access denied");

        return project == null ? null : ProjectMapper.ToProjectDto(project);
    }

    public async Task<ProjectDto> CreateProjectAsync(CreateProjectDto dto)
{
    if (dto == null) throw new ArgumentNullException(nameof(dto));

    var tenantId = _tenantAccessor.TenantId;

    bool exists = await _context.Projects
        .AnyAsync(p => p.TenantId == tenantId && p.Name == dto.Name);
    if (exists)
        throw new InvalidOperationException($"Project '{dto.Name}' already exists.");

    var project = dto.ToProjectModel();
    project.TenantId = tenantId;

    _context.Projects.Add(project);
    await _context.SaveChangesAsync();

    return ProjectMapper.ToProjectDto(project);
}

    public async Task<ProjectDto> UpdateProjectAsync(int projectId, UpdateProjectDto dto)
    {
        if (dto == null) throw new ArgumentNullException(nameof(dto));

        // var existingProject = await GetProjectByIdAsync(projectId);
        var existingProject = await _context.Projects
            .FirstOrDefaultAsync(p => p.Id == projectId && p.TenantId == _tenantAccessor.TenantId);
        if (existingProject == null)
        {
            throw new KeyNotFoundException($"Project {dto.Id} not found.");
        }

        existingProject.UpdateFromDto(dto);
        await _context.SaveChangesAsync();

        return ProjectMapper.ToProjectDto(existingProject);
    }


    public async Task<bool> DeleteProjectAsync(int projectId)
    {
        // var project = await GetProjectByIdAsync(projectId);
        var project = await _context.Projects
            .FirstOrDefaultAsync(p => p.Id == projectId && p.TenantId == _tenantAccessor.TenantId);
        if (project == null) return false;

        _context.Projects.Remove(project);
        await _context.SaveChangesAsync();

        return true;
    }

}