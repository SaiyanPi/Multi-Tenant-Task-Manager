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

                // -> Returns true if no user with the email exists (i.e., the email is available for use),
                // -> Returns false if a user already exists with that email.
                if (dto.Role == "SuperAdmin")
                {
                    var existing = await userManager.Users.Where(u => u.Email == email && u.TenantId == null)
                    .SingleOrDefaultAsync(cancellation);
                    return existing == null; // returns false if email exist
                }

                // For tenant users, check if email exists within the same tenant
                // If a user exists → AnyAsync returns true | !true → false
                // If no user is found → AnyAsync returns false | !false → true
                return !await userManager.Users
                    .Where(u => u.Email == email && u.TenantId == tenantId)
                    .AnyAsync(cancellation);

            }).WithMessage("Email is already in use.");

        RuleFor(u => u.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(6).WithMessage("Password must be at least 6 characters long.");

        RuleFor(u => u.TenantId)
            .MustAsync(async (tenantId, cancellation) =>
            {
                if (!tenantId.HasValue || tenantId == Guid.Empty)
                    return true; // Let controller handle required check

                return await dbContext.Tenants.AnyAsync(t => t.Id == tenantId.Value, cancellation);

            }).WithMessage("Tenant does not exist.")
            .When(u => u.Role is not null and not "SuperAdmin"); // Skip this check for SuperAdmin
    }
}
