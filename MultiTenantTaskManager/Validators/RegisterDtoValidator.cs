using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MultiTenantTaskManager.Accessor;
using MultiTenantTaskManager.Authentication;
using MultiTenantTaskManager.Authentication.DTOs;
using MultiTenantTaskManager.Data;

namespace MultiTenantTaskManager.Validators;

public class RegisterDtoValidator : AbstractValidator<RegisterDto>
{
    public RegisterDtoValidator(ApplicationDbContext dbContext, UserManager<ApplicationUser> userManager,
        ITenantAccessor tenantAccessor)
    {
        RuleFor(u => u.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("A valid email is required.")
            .MustAsync(async (dto, email, cancellation) =>
            {
                var tenantId = tenantAccessor.TenantId;
                // SuperAdmin is global, check globally
                if (dto.Role == "SuperAdmin")
                {
                    var existing = await userManager.FindByEmailAsync(email);
                    return existing == null;
                }

                // For tenant users, check if email exists within the same tenant
                // if provided TenantId does not match with the header TenantId, return an error
                if (!dto.TenantId.HasValue || dto.TenantId == Guid.Empty)
                {
                    dto.TenantId = tenantAccessor.TenantId;
                }
                // Fallback to tenant accessor if TenantId is not provided in body
                if (!dto.TenantId.HasValue || dto.TenantId == Guid.Empty)
                {
                    dto.TenantId = tenantAccessor.TenantId;
                }

                return !await userManager.Users
                    .Where(u => u.Email == email && u.TenantId == tenantId)
                    .AnyAsync(cancellation);

            }).WithMessage("Email is already in use.");

        RuleFor(u => u.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(6).WithMessage("Password must be at least 6 characters long.");

        RuleFor(u => u.TenantId)
            .NotEmpty().WithMessage("Tenant ID is required.")
            .MustAsync(async (tenantId, cancellation) =>
            {
                if (!tenantId.HasValue || tenantId == Guid.Empty)
                    return false;

                return await dbContext.Tenants.AnyAsync(t => t.Id == tenantId.Value, cancellation);
            }).WithMessage("Tenant with the given ID does not exist.")
            .When(u => u.Role is not null and not "SuperAdmin"); // Skip this check for SuperAdmin
    }
}
