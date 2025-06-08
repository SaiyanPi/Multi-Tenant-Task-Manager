using Microsoft.EntityFrameworkCore;
using MultiTenantTaskManager.Accessor;
using MultiTenantTaskManager.Data;
using MultiTenantTaskManager.Models;

namespace MultiTenantTaskManager.Services;
public class ProjectService : IProjectService
{
    private readonly ApplicationDbContext _context;
    private readonly ITenantAccessor _tenantAccessor;

    public ProjectService(ApplicationDbContext context, ITenantAccessor tenantAccessor)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _tenantAccessor = tenantAccessor ?? throw new ArgumentNullException(nameof(tenantAccessor));
    }

    public async Task<IEnumerable<Project>> GetAllProjectsAsync()
    {
        return await _context.Projects
            .Where(p => p.TenantId == _tenantAccessor.TenantId)
            .Include(p => p.Tasks)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<Project?> GetProjectByIdAsync(int projectId)
    {
        return await _context.Projects
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == projectId && p.TenantId == _tenantAccessor.TenantId);
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