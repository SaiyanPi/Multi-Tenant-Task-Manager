using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MultiTenantTaskManager.Accessor;
using MultiTenantTaskManager.Authentication;
using MultiTenantTaskManager.Data;
using MultiTenantTaskManager.DTOs.TaskItem;

namespace MultiTenantTaskManager.Validators;

public class AssignUserToTaskDtoValidator : AbstractValidator<AssignUserToTaskDto>
{
    public AssignUserToTaskDtoValidator(ApplicationDbContext dbContext, UserManager<ApplicationUser> userManager,
        ITenantAccessor tenantAccessor)
    {
        RuleFor(x => x.TaskItemId)
            .NotEmpty()
            .MustAsync(async (taskId, cancellation) =>
            {
                var tenantId = tenantAccessor.TenantId;
                return await dbContext.TaskItems
                    .AnyAsync(t => t.Id == taskId && t.TenantId == tenantId, cancellation);
            })
            .WithMessage("Invalid or unauthorized task item ID.");

        RuleFor(x => x.AssignedUser)
            .NotEmpty()
            .MustAsync(async (userId, ct) =>
            {
                var tenantId = tenantAccessor.TenantId;
                var user = await userManager.FindByIdAsync(userId);
                return user != null && user.TenantId == tenantId;
            })
            .WithMessage("Invalid or unauthorized user.");
    }
    
}
