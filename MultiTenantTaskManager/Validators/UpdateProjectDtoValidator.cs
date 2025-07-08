using System.Data;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using MultiTenantTaskManager.Accessor;
using MultiTenantTaskManager.Data;
using MultiTenantTaskManager.DTOs.Project;

namespace MultiTenantTaskManager.Validators;
public class UpdateProjectDtoValidator : AbstractValidator<UpdateProjectDto>
{
    public UpdateProjectDtoValidator(ApplicationDbContext dbContext, ITenantAccessor tenantAccessor)
    {
        RuleFor(p => p.Id)
            .Cascade(CascadeMode.Stop) // Stop validating if Id fails
            .NotEmpty().WithMessage("Project Id is required.")
            .GreaterThan(0).WithMessage("Invalid project ID.");

        RuleFor(p => p.Name)
            .NotEmpty().WithMessage("Project Name is required.")
            .MaximumLength(100).WithMessage("Title must not exceed 100 characters.")
            .MustAsync(async (dto, name, cancellation) =>
            {
                var tenantId = tenantAccessor.TenantId;
                return !await dbContext.Projects.AnyAsync(p => p.Name == name && p.Id != dto.Id && p.TenantId == tenantId);
            }).WithMessage("A project with this name already exists in your tenant.");
            //.When(p => p.Id > 0); // prevents running if ID is invalid
        
        RuleFor(t => t.Description)
            .NotEmpty().WithMessage("Task description is required")
            .MinimumLength(10).WithMessage("Description must be at least 10 characters.")
            .MaximumLength(1000).WithMessage("Description must not exceed 1000 characters.");
        
        RuleFor(t => t.DueDate)
            .NotNull().WithMessage("Due date is required.")
            .GreaterThanOrEqualTo(DateTime.Today).WithMessage("Due date cannot be in the past.");
    }
}

// if we remove the '.Cascade(CascadeMode.Stop)' and '.When(p => p.Id > 0)' then regardless of in which
// property the failure is, we will get messages from all property in the response. This is because 
// FluentValidation runs all rules even if the earlier ones fail.
// for example, if we make a request with id = 0, the expected response will be "Invalid project ID."
// but the response we will get will be something like:
// "title": "One or more validation errors occurred.",
//     "status": 400,
//     "errors": {
//         "id": [
//             "Invalid project ID."
//         ],
//         "Name": [
//             "A project with this name already exists in your tenant."
//         ]
//     }

// the solution to this is to Use .Cascade(CascadeMode.Stop) and When(...) properly
// Strategy :
// 1. Make Id the "gatekeeper" — if Id is invalid, skip the rest.
// 2. Use .When(x => x.Id > 0) on all other rules.
// 3. Combine .Cascade(CascadeMode.Stop) for each property to avoid chaining multiple failures.

// This will work even if the id is valid and some other properties are invalid(only returns the messages
// from invalid properties)
