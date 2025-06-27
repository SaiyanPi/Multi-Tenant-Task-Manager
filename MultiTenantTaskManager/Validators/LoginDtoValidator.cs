using FluentValidation;
using Microsoft.AspNetCore.Identity;
using MultiTenantTaskManager.Accessor;
using MultiTenantTaskManager.Authentication;
using MultiTenantTaskManager.Authentication.DTOs;
using MultiTenantTaskManager.Data;
using Microsoft.EntityFrameworkCore;

namespace MultiTenantTaskManager.Validators;
public class LoginDtoValidator : AbstractValidator<LoginDto>
{
    public LoginDtoValidator(ApplicationDbContext dbContext, UserManager<ApplicationUser> userManager,
        ITenantAccessor tenantAccessor)
    {
        RuleFor(u => u.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("A valid email is required.")
            .MustAsync(async (email, cancellation) =>
            {
                // var user = await userManager.FindByEmailAsync(email);

                // The default ASP.NET Identity UserManager assumes emails are globally unique. But this is
                // a multi-tenant app therefore, multiple users with the same email but different t
                // Instead, we need to query the users filtered by email AND tenant ID.

                var userByEmail = await userManager.Users
                    .Where(u => u.Email == email)
                    .ToListAsync();

                if (userByEmail.Count == 0)
                    return false;

                return await userManager.Users
                    .Where(u => u.Email == email)
                    .AnyAsync(cancellation); // returns true if user and tenantId exist

            }).WithMessage("Email not registered");

        RuleFor(u => u.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(6).WithMessage("Password must be at least 6 characters long.");
    }
}
