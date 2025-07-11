using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration.UserSecrets;
using MultiTenantTaskManager.Accessor;
using MultiTenantTaskManager.Authentication;
using MultiTenantTaskManager.Data;
using MultiTenantTaskManager.DTOs;
using MultiTenantTaskManager.DTOs.Project;
using MultiTenantTaskManager.Enums;
using MultiTenantTaskManager.Helpers;
using MultiTenantTaskManager.Mappers;
using MultiTenantTaskManager.Models;
using MultiTenantTaskManager.Validators;

namespace MultiTenantTaskManager.Services;

public class ProjectService : TenantAwareService, IProjectService
{
    private readonly ApplicationDbContext _context;
    // private readonly ITenantAccessor _tenantAccessor;
    // private readonly IAuthorizationService _authorizationService;
    // private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IAuditService _auditService;
    private readonly IUserAccessor _userAccessor;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly INotificationService _notificationService;


    public ProjectService(
        ApplicationDbContext context,
        ClaimsPrincipal user,
        ITenantAccessor tenantAccessor,
        IAuthorizationService authorizationService,
        IAuditService auditService,
        IUserAccessor userAccessor,
        UserManager<ApplicationUser> userManager,
        INotificationService notificationService)
        : base(user, tenantAccessor, authorizationService)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));
        // _tenantAccessor = tenantAccessor ?? throw new ArgumentNullException(nameof(tenantAccessor));
        // _authorizationService = authorizationService ?? throw new ArgumentNullException(nameof(authorizationService));
        // _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        _userAccessor = userAccessor ?? throw new ArgumentNullException(nameof(userAccessor));
        _userManager = userManager;
        _notificationService = notificationService;

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
            .Include(p => p.AssignedUsers)
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
            .Include(p => p.AssignedUsers)
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

    public async Task<ProjectDto> AssignUsersToProjectAsync(AssignUsersToProjectDto dto)
    {
        var tenantId = _tenantAccessor.TenantId;

        var project = await _context.Projects
            .Include(p => p.AssignedUsers)
            .FirstOrDefaultAsync(p => p.Id == dto.ProjectId && p.TenantId == tenantId);

        if (project == null)
            throw new KeyNotFoundException("Project not found.");

        var userIds = dto.AssignedUsers.Keys.ToList();

        var users = await _userManager.Users
            .Where(u => userIds.Contains(u.Id) && u.TenantId == tenantId && !u.IsDeleted)
            .ToListAsync();

        if (users.Count != dto.AssignedUsers.Count)
            throw new ArgumentException("Some users are invalid or not in the tenant.");

        // var specialMemberCount = dto.UserRoles.Values.Count(r => r == AppRoles.SpecialMember);
        // if (specialMemberCount > 1)
        //     throw new InvalidOperationException("Only one SpecialMember is allowed per project.");

        // var managerCount = dto.UserRoles.Values.Count(r => r == AppRoles.Manager);
        // if (managerCount > 1)
        //     throw new InvalidOperationException("Only one Manager is allowed per project.");

        // --- for auditlog ---
        var previousAssignedUsersId = project.AssignedUsers?.Select(u => new ProjectUserDto
        {
            UserId = u.Id,
            Email = u.Email ?? string.Empty,
            // RoleInProject = u.RoleInProject ?? string.Empty
        }).ToList() ?? new();
        var newAssignedUsersId = users.Select(u => new ProjectUserDto
        {
            UserId = u.Id,
            Email = u.Email ?? string.Empty,
            // RoleInProject = u.RoleInProject ?? string.Empty
        }).ToList() ?? new();
        // ------------------

        // set task properties after assigning user
        project.Status = ProjectStatus.Assigned;
        
        // set users properties after assigning users
        foreach (var user in users)
        {
            user.ProjectId = project.Id;
            user.RoleInProject = dto.AssignedUsers[user.Id];
        }

        await _context.SaveChangesAsync();

        // call SignalR notification
        var projectName = project.Name;
        foreach (var userId in userIds)
        {
            await _notificationService.SendNotificationAsync(userId, "New Project Assigned", $"You've been assigned to project '{projectName}'");
        }

        // auditlog
        var auditData = new
        {
            // PreviousAssignedUser = task.AssignedUserId,
            PreviousAssignedUsers = previousAssignedUsersId,
            NewAssignedUsers = newAssignedUsersId
        };

        // differentiating Reassigned and AssignUser actions based on condition
        string actionValue = previousAssignedUsersId != null ? "Reassigned" : "AssignUser";

        await _auditService.LogAsync(
           action: actionValue,
           entityName: "Project",
           entityId: project.Id.ToString(),
           changes: JsonSerializer.Serialize(auditData)
       );

        return project.ToProjectDto(); // make sure this returns assigned users
    }

    public async Task<bool> UpdateProjectStatusAsync(int projectId, UpdateProjectStatusDto dto)
    {
        var tenantId = _tenantAccessor.TenantId;

        var project = await _context.Projects.FirstOrDefaultAsync(p => p.Id == projectId && p.TenantId == tenantId);

        if (project == null)
        {
            throw new Exception("Project not found.");
        }

        // Prevent updates once project is completed
        if (project.Status == ProjectStatus.Completed)
            throw new InvalidOperationException("Cannot update status. The project has already been completed.");

         // auditlog: original task status snapshot before PATCH  
        var originalProjectStatusDto = project.Status;

        // calling custom UpdateProjectStatusValidator -------
        var currentStatus = project.Status;
        var validator = new UpdateProjectStatusDtoValidator();
        var result = validator.ValidateWithContext(dto, currentStatus);

        if (!result.IsValid)
        {
            throw new InvalidOperationException($"Invalid status update: {string.Join(", ", result.Errors.Select(e => e.ErrorMessage))}");
        }
        // --------

        // since my NewStatus is a string type, Parse the new status from string to enum
        var validStatuses = string.Join(", ", Enum.GetNames(typeof(ProjectStatus)));
        if (!Enum.TryParse<ProjectStatus>(dto.NewStatus, out var newStatus))
            throw new InvalidOperationException($"Invalid status value: {dto.NewStatus}. Valid status values are: {validStatuses}.");

        // validate status transition
        if (!ProjectStatusTransition.CanTransition(project.Status, newStatus))
            throw new InvalidOperationException($"Cannot transition from {project.Status} to {newStatus}");

        // Set timestamps based on status
        if (project.Status != newStatus)
        {
            if (newStatus == ProjectStatus.InProgress)
                project.StartedAt = DateTime.UtcNow;
            else if (newStatus == ProjectStatus.Completed)
                project.CompletedAt = DateTime.UtcNow;
        }
        project.Status = newStatus;
        await _context.SaveChangesAsync();

        // auditlog: refetch after patch
        var updatedProject = await _context.Projects.FirstOrDefaultAsync(p => p.Id == projectId && p.TenantId == tenantId);
        var updatedProjectStatusDto = updatedProject!.Status;

        var auditData = new
        {
            Original = originalProjectStatusDto,
            Updated = updatedProjectStatusDto
        };

        string actionValue = dto.NewStatus switch
        {
            nameof(ProjectStatus.InProgress) => "StartProject",
            nameof(ProjectStatus.Completed)  => "CompleteProject",
            _ => throw new InvalidOperationException($"Unsupported status: {dto.NewStatus}") // fallback if other statuses are possible
        };

        await _auditService.LogAsync(
            action: actionValue,
            entityName: "Project",
            entityId: project.Id.ToString(),
            changes: JsonSerializer.Serialize(auditData)
        );
        // ------

        return true;





        
    }


}