using FluentValidation;
using Microsoft.EntityFrameworkCore;
using MultiTenantTaskManager.Accessor;
using MultiTenantTaskManager.Data;
using MultiTenantTaskManager.DTOs.Tenant;

namespace MultiTenantTaskManager.Validators;
public class CreateTenantDtoValidator : AbstractValidator<CreateTenantDto>
{

    public CreateTenantDtoValidator(ApplicationDbContext dbContext)
    {
        RuleFor(t => t.Name)
            .NotEmpty().WithMessage("Tenant name is required.")
            .MaximumLength(100).WithMessage("Tenant name must not exceed 100 characters.")
            .MustAsync(async (name, cancellation) =>
            {
                return !await dbContext.Tenants.AnyAsync(t => t.Name == name);
            }).WithMessage("Tenant with this name already exists.");

    }
}