using FluentValidation;
using Microsoft.EntityFrameworkCore;
using MultiTenantTaskManager.Data;
using MultiTenantTaskManager.DTOs.Tenant;

namespace MultiTenantTaskManager.Validators;

public class UpdateTenantDtoValidator : AbstractValidator<UpdateTenantDto>
{

    public UpdateTenantDtoValidator(ApplicationDbContext dbContext)
    {
        RuleFor(t => t.Id)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Tenant Id is required.");

        RuleFor(t => t.Name)
            .NotEmpty().WithMessage("Tenant name is required.")
            .MaximumLength(100).WithMessage("Tenant name must not exceed 100 characters.")
            .MustAsync(async (dto, name, cancellation) =>
            {
                return !await dbContext.Tenants.AnyAsync(t => t.Name == name && t.Id != dto.Id);
            }).WithMessage("Tenant with this name already exists");

    }
}