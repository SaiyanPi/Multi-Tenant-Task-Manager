using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MultiTenantTaskManager.Accessor;
using MultiTenantTaskManager.Authentication;
using MultiTenantTaskManager.Data;
using MultiTenantTaskManager.DTOs.User;

namespace MultiTenantTaskManager.Validators;

public class UpdateUserDtoValidator : AbstractValidator<UpdateUserDto>
{
    // private static readonly string[] ValidRoles =
    // {
    //     AppRoles.SuperAdmin,
    //     AppRoles.Admin,
    //     AppRoles.Manager,
    //     AppRoles.Member,
    //     AppRoles.SpecialMember
    // };
    // public UpdateUserDtoValidator(IUserAccessor userAccessor, UserManager<ApplicationUser> userManager)
    // {
    //     RuleFor(x => x.Role)
    //         .Cascade(CascadeMode.Stop)
    //         .NotEmpty().WithMessage("Role is required.")

    //         // 1. Check that role is valid
    //         .Must(role => ValidRoles.Contains(role))
    //         .WithMessage($"Invalid role. Allowed roles are: {string.Join(", ", ValidRoles)}")

    //         // 2. Check that only SuperAdmin can assign SuperAdmin
    //         .MustAsync(async (role, cancellation) =>
    //         {
    //             // Only enforce this rule if role is SuperAdmin
    //             if (role != AppRoles.SuperAdmin)
    //                 return true;

    //             var currentUser = await userManager.FindByIdAsync(userAccessor.UserId.ToString());
    //             if (currentUser == null) return false;

    //             var currentRoles = await userManager.GetRolesAsync(currentUser);
    //             return currentRoles.Contains(AppRoles.SuperAdmin);
    //         })
    //         .WithMessage("You do not have permission to assign the SuperAdmin role.");
    // }

    // Dynamic
    public UpdateUserDtoValidator(IUserAccessor userAccessor, UserManager<ApplicationUser> userManager,
    RoleManager<IdentityRole> roleManager)
    {
        var validRoles = roleManager.Roles.Select(r => r.Name!).ToList();

        RuleFor(x => x.Role)
        .Cascade(CascadeMode.Stop)
        .NotEmpty().WithMessage("Role is required.")
        .Must(role => validRoles.Contains(role))
        .WithMessage($"Invalid role. Allowed roles are: {string.Join(", ", validRoles)}")

        // .MustAsync(async (role, ct) =>
        // {
        //     var roleExists = await roleManager.RoleExistsAsync(role);
        //     return roleExists;
        // })
        // .WithMessage("Invalid role. Please choose a valid role from the system.")
        
        // Validate SuperAdmin can only be assigned by SuperAdmin
        .MustAsync(async (role, ct) =>
        {
            if (role != AppRoles.SuperAdmin)
                return true;

            var userId = userAccessor.UserId.ToString();
            var currentUser = await userManager.FindByIdAsync(userId);
            if (currentUser == null) return false;

            var currentRoles = await userManager.GetRolesAsync(currentUser);
            return currentRoles.Contains(AppRoles.SuperAdmin);
        })
        .WithMessage("You do not have permission to assign the SuperAdmin role.");
    }
}
