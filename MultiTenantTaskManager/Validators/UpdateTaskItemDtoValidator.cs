using FluentValidation;
using Microsoft.EntityFrameworkCore;
using MultiTenantTaskManager.Accessor;
using MultiTenantTaskManager.Data;
using MultiTenantTaskManager.DTOs.TaskItem;

namespace MultiTenantTaskManager.Validators;
public class UpdateTaskItemDtoValidator : AbstractValidator<UpdateTaskItemDto>
{

    public UpdateTaskItemDtoValidator(ApplicationDbContext dbContext, ITenantAccessor tenantAccessor)
    {
        RuleFor(t => t.Id)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Task Id is required.")
            .GreaterThan(0).WithMessage("Invalid task item ID.");

        RuleFor(t => t.Titles)
            .NotEmpty().WithMessage("Task Item title is required.")
            .MaximumLength(100).WithMessage("Title must not exceed 100 characters.")
            .MustAsync(async (dto, title, cancellation) =>
            {
                var tenantId = tenantAccessor.TenantId;
                return !await dbContext.TaskItems.AnyAsync(t => t.Titles == title && t.Id != dto.Id && t.TenantId == tenantId);
            }).WithMessage("Task Item with this title already exists in your tenant.")
            .When(t => t.Id > 0);

        RuleFor(t => t.ProjectId)
            .NotEmpty().WithMessage("Project Id is required.")
            .When(t => t.Id > 0);
    }
}