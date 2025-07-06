using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MultiTenantTaskManager.Accessor;
using MultiTenantTaskManager.Authentication;
using MultiTenantTaskManager.Data;
using MultiTenantTaskManager.DTOs.Project;

namespace MultiTenantTaskManager.Validators;

public class AssignUsersToProjectDtoValidator : AbstractValidator<AssignUsersToProjectDto>
{
    private readonly ApplicationDbContext _context;
    private readonly ITenantAccessor _tenantAccessor;

    public AssignUsersToProjectDtoValidator(ApplicationDbContext dbContext, UserManager<ApplicationUser> userManager,
        ITenantAccessor tenantAccessor)
    {
        _context = dbContext;
        _tenantAccessor = tenantAccessor;

        RuleFor(x => x.ProjectId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (projectId, cancellation) =>
            {
                var tenantId = tenantAccessor.TenantId;
                return await dbContext.Projects
                    .AnyAsync(p => p.Id == projectId && p.TenantId == tenantId, cancellation);
            })
            .WithMessage("Invalid or unauthorized project ID.");

        RuleFor(x => x.AssignedUsers)
            .Cascade(CascadeMode.Stop)
            .NotNull()
            .WithMessage("'Assigned Users' must not be empty.")
            .Must(r => r.Any())
            .WithMessage("At least one user must be assigned to the project.");

        RuleFor(x => x.AssignedUsers)
            .Cascade(CascadeMode.Stop)
            .Must(AllRolesAreValid)
            .WithMessage("One or more roles are invalid.")
            .When(x => x.AssignedUsers != null && x.AssignedUsers.Any());

        RuleFor(x => x.AssignedUsers)
            .Cascade(CascadeMode.Stop)
            .Must(NoAdminsAssigned)
            .WithMessage("Admin users cannot be assigned to projects.")
            .When(x => x.AssignedUsers != null && x.AssignedUsers.Any()); // only run if there are users

        RuleFor(x => x.AssignedUsers)
            .Cascade(CascadeMode.Stop)
            .MustAsync(UserIdsMustBeValidAndSameTenant)
            .WithMessage("One or more users are invalid or do not belong to the same tenant.")
            .When(x => x.AssignedUsers != null && x.AssignedUsers.Any());
    }

    private bool AllRolesAreValid(Dictionary<string, string> userRoles)
    {
        return userRoles.All(kv => AppRoles.ValidProjectAssignableRoles.Contains(kv.Value));
    }
    private async Task<bool> UserIdsMustBeValidAndSameTenant(Dictionary<string, string> AssignedUsers, CancellationToken cancellationToken)
    {
        var tenantId = _tenantAccessor.TenantId;
        var userIds = AssignedUsers.Keys.ToList();

        var users = await _context.Users
            .Where(u => userIds.Contains(u.Id) && u.TenantId == tenantId && !u.IsDeleted)
            .Select(u => u.Id)
            .ToListAsync(cancellationToken);

        // Ensure all userIds provided exist and match the tenant
        return users.Count == userIds.Count;
    }

    private bool NoAdminsAssigned(Dictionary<string, string> userRoles)
    {
        return userRoles.All(kv => !string.Equals(kv.Value, AppRoles.Admin, StringComparison.OrdinalIgnoreCase));
    }
}