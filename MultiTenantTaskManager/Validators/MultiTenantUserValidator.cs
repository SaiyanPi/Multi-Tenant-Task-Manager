using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MultiTenantTaskManager.Accessor;
using MultiTenantTaskManager.Authentication;

namespace  MultiTenantTaskManager.Validators;
public class MultiTenantUserValidator : UserValidator<ApplicationUser>
{
    private readonly ITenantAccessor _tenantAccessor;

    public MultiTenantUserValidator(IdentityErrorDescriber errors, ITenantAccessor tenantAccessor)
        : base(errors)
    {
        _tenantAccessor = tenantAccessor;
    }

    public override async Task<IdentityResult> ValidateAsync(UserManager<ApplicationUser> manager,
        ApplicationUser user)
    {
        Console.WriteLine("MultiTenantUserValidator invoked...");
        
        var errors = new List<IdentityError>();
        var tenantId = _tenantAccessor.TenantId;

        // Check email uniqueness within the same tenant
        var duplicateUser = await manager.Users
            .AnyAsync(u => u.Email == user.Email && u.TenantId == tenantId && u.Id != user.Id);

        if (duplicateUser)
        {
            errors.Add(new IdentityError
            {
                Code = "DuplicateEmail",
                Description = $"Email '{user.Email}' is already taken in this tenant."
            });
        }

        return errors.Count == 0
            ? IdentityResult.Success
            : IdentityResult.Failed(errors.ToArray());
    }
}
