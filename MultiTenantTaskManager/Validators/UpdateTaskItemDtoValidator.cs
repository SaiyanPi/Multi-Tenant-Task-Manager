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
            .MaximumLength(100).WithMessage("Title must not exceed 100 characters.");
            //.When(t => t.Id > 0);

        RuleFor(t => t.Description)
            .NotEmpty().WithMessage("Task description is required")
            .MinimumLength(10).WithMessage("Description must be at least 10 characters.")
            .MaximumLength(1000).WithMessage("Description must not exceed 1000 characters.");

        RuleFor(t => t.ProjectId)
            .NotEmpty().WithMessage("Project Id is required.");
            //.When(t => t.Id > 0);
        
        RuleFor(t => t.DueDate)
            .NotNull().WithMessage("Due date is required.")
            .GreaterThanOrEqualTo(DateTime.Today).WithMessage("Due date cannot be in the past.");
    }
}