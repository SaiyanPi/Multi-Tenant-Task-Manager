using FluentValidation;
using Microsoft.EntityFrameworkCore;
using MultiTenantTaskManager.Accessor;
using MultiTenantTaskManager.Data;
using MultiTenantTaskManager.DTOs.Project;

namespace MultiTenantTaskManager.Validators;

public class CreateProjectDtoValidator : AbstractValidator<CreateProjectDto>
{
    // This only validate the Project name so this is commented
    // public CreateProjectDtoValidator()
    // {
    //     RuleFor(p => p.Name)
    //         .NotEmpty().WithMessage("Project Name is required.")
    //         .MaximumLength(100).WithMessage("Title must not exceed 100 characters.");
    // }



    // this also checks the duplicate project name inside the tenant.
    public CreateProjectDtoValidator(ApplicationDbContext dbContext, ITenantAccessor tenantAccessor)
    {
        RuleFor(p => p.Name)
            .NotEmpty().WithMessage("Project Name is required.")
            .MaximumLength(100).WithMessage("Title must not exceed 100 characters.")
            // if this delegate return true, validation passes
            .MustAsync(async (name, cancellation) => 
            {
                var tenantId = tenantAccessor.TenantId;
                // if the project with the same tenantId exists(true), returns false(!true is false) and validation fails
                return !await dbContext.Projects.AnyAsync(p => p.Name == name && p.TenantId == tenantId);

            }).WithMessage("Project title already exists in your tenant.");
    }
}

// We've enforced the buisness logic for Unique project name(inside the ProjectService class) with above
// fluent validation. Note that Fluent validation is independent of business logic.

// Both business logic and fluent logic for unique project name within the tenant are quite similar but
// If we check the the business logic we will find the use of CreateProjectDto as: 
// (p => p.TenantId == tenantId && p.Name == dto.Name)
// But here in the fluent logic, we don't see the dto
// (p => p.Name == name && p.TenantId == tenantId)
// WHY?
// It is because FluentValidation works at the property level, not at the DTO level. But still we can use
// dto inside fluent (check out the project update validator)


// NOTE: FLUENT VALIDATION does not run automatically in service methods we have to inject it.
// for example in the context of updating the user:
//  - it does not have a controller action unlike updating project,.. which have a controller action. so we have
//      to inject the UpdateUserDtoValidator in the User service class.
