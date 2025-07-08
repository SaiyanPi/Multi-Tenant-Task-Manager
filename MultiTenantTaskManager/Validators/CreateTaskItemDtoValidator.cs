using FluentValidation;
using Microsoft.EntityFrameworkCore;
using MultiTenantTaskManager.Accessor;
using MultiTenantTaskManager.Data;
using MultiTenantTaskManager.DTOs.TaskItem;

namespace MultiTenantTaskManager.Validators;
public class CreateTaskItemDtoValidator : AbstractValidator<CreateTaskItemDto>
{

    public CreateTaskItemDtoValidator(ApplicationDbContext dbContext, ITenantAccessor tenantAccessor)
    {
        RuleFor(t => t.Titles)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Task Item title is required.")
            .MaximumLength(100).WithMessage("Title must not exceed 100 characters.")
            .MustAsync(async (title, cancellation) =>
            {
                var tenantId = tenantAccessor.TenantId;
                return !await dbContext.TaskItems.AnyAsync(t => t.Titles == title && t.TenantId == tenantId);
            }).WithMessage("Task Item with this title already exists in your tenant.");

        RuleFor(t => t.Description)
            .NotEmpty().WithMessage("Task description is required")
            .MinimumLength(10).WithMessage("Description must be at least 10 characters.")
            .MaximumLength(1000).WithMessage("Description must not exceed 1000 characters.");

        RuleFor(t => t.ProjectId)
            .NotEmpty().WithMessage("Project Id is required.");
            //.When(x => !string.IsNullOrWhiteSpace(x.Titles) && x.Titles.Length >= 100);
        
        RuleFor(t => t.DueDate)
            .NotNull().WithMessage("Due date is required.")
            .GreaterThanOrEqualTo(DateTime.Today).WithMessage("Due date cannot be in the past.");
    }
}