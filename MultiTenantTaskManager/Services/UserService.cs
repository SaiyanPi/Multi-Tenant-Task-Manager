using System.Security.Claims;
using System.Text.Json;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MultiTenantTaskManager.Accessor;
using MultiTenantTaskManager.Authentication;
using MultiTenantTaskManager.DTOs.User;
using MultiTenantTaskManager.Mappers;
using MultiTenantTaskManager.Models;

namespace MultiTenantTaskManager.Services;

public class UserService : TenantAwareService, IUserService
{
    private readonly UserManager<ApplicationUser> _userManager;
        private readonly IAuditService _auditService;
        private readonly IUserAccessor _userAccessor; // inject UpdateUserDtoValidator because it does not have a controller action
    private readonly IValidator<UpdateUserDto> _updateUserDtoValidator;



    public UserService(
        UserManager<ApplicationUser> userManager,
        IAuditService auditService,
        IUserAccessor userAccessor,
        ClaimsPrincipal user,
        ITenantAccessor tenantAccessor,
        IAuthorizationService authorizationService,
        IValidator<UpdateUserDto> updateUserDtoValidator
       ) : base(user, tenantAccessor, authorizationService)
    {
        _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        _auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));
        _userAccessor = userAccessor ?? throw new ArgumentNullException(nameof(userAccessor));
        _updateUserDtoValidator = updateUserDtoValidator;
    }

    public async Task<IEnumerable<UserDto>> GetAllUsersForTenantAsync(int page = 1, int pageSize = 10)
    {
        await AuthorizeSameTenantAsync();

        var tenantId = GetCurrentTenantId();

        // Filter users by tenantId
        var users = await _userManager.Users
            .AsNoTracking()
            .Where(u => u.TenantId == tenantId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var userDtos = new List<UserDto>();

        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            // userDtos.Add(MapToUserDto(user, roles));
            userDtos.Add(UserMapper.ToUserDto(user, roles));

        }

        return userDtos;
    }

    public async Task<UserDto> GetUserByIdAsync(string userId)
    {
        await AuthorizeSameTenantAsync();

        var tenantId = GetCurrentTenantId();

        var user = await _userManager.Users
            .FirstOrDefaultAsync(u => u.Id == userId && u.TenantId == tenantId);

        if (user == null) throw new InvalidOperationException($"User with ID '{userId}' not found.");

        var roles = await _userManager.GetRolesAsync(user);
        // return MapToUserDto(user, roles);
        return UserMapper.ToUserDto(user, roles);

    }

    public async Task<UserDto> UpdateUserAsync(string userId, UpdateUserDto dto)
    {
        await AuthorizeSameTenantAsync();

        var tenantId = GetCurrentTenantId();


        if (dto == null) throw new ArgumentNullException(nameof(dto));

        var result = await _updateUserDtoValidator.ValidateAsync(dto); // calling the UpdateUserDtoValidator
        if (!result.IsValid)
        throw new ValidationException(result.Errors);

        var user = await _userManager.Users
            .FirstOrDefaultAsync(u => u.Id == userId && u.TenantId == tenantId);
        if (user == null)
            throw new InvalidOperationException("User with the given ID was not found.");

        // original user DTO snapshot before update
        var originalUserDto = UserMapper.ToUserDto(user, await _userManager.GetRolesAsync(user));

        // Now retrieve it again for tracking changes
        var trackedUser = await _userManager.Users
            .FirstOrDefaultAsync(u => u.Id == userId && u.TenantId == tenantId);
        if (trackedUser == null)
            throw new InvalidOperationException($"User with ID '{userId}' not found for update.");

        // no use because of fluent validation(UpdateUserDtoValidator)
        // // Validate roles
        // var validRoles = new[] {
        //     AppRoles.SuperAdmin,
        //     AppRoles.Admin,
        //     AppRoles.Manager,
        //     AppRoles.Member,
        //     AppRoles.SpecialMember
        // };

        // // Validate the provided role
        // if (!validRoles.Contains(dto.Role))
        //     throw new InvalidOperationException($"Invalid role '{dto.Role}'. Allowed roles are: {string.Join(", ", validRoles)}");

        // Remove all existing roles
        var currentRoles = await _userManager.GetRolesAsync(trackedUser);
        var removeResult = await _userManager.RemoveFromRolesAsync(trackedUser, currentRoles);
        if (!removeResult.Succeeded)
            throw new InvalidOperationException($"Failed to remove existing roles from user '{userId}'.");

        // Add the new role
        var addResult = await _userManager.AddToRoleAsync(trackedUser, dto.Role);
        if (!addResult.Succeeded)
            throw new InvalidOperationException($"Failed to assign role '{dto.Role}' to user '{userId}'.");

        // User DTO after update
        var updatedUserDto = UserMapper.ToUserDto(trackedUser, new List<string> { dto.Role });

        // Log both old and new state
        var auditData = new
        {
            Original = originalUserDto,
            Updated = updatedUserDto
        };
        await _auditService.LogAsync(
            action: "Update",
            entityName: "ApplicationUser",
            entityId: user.Id.ToString(),
            changes: JsonSerializer.Serialize(auditData)
        );

        // return MapToUserDto(user, new List<string> { dto.Role });
        return UserMapper.ToUserDto(user, new List<string> { dto.Role });

    }

    public async Task<bool> DeleteUserAsync(string userId)
    {
        await AuthorizeSameTenantAsync();

        var tenantId = GetCurrentTenantId();

        var user = await _userManager.Users
        .FirstOrDefaultAsync(u => u.Id == userId && u.TenantId == tenantId);

        if (user == null)
            throw new InvalidOperationException("User with the given ID was not found.");

        // User DTO before deletion
        var deletedUserDto = UserMapper.ToUserDto(user, await _userManager.GetRolesAsync(user));

        // var deleteResult = await _userManager.DeleteAsync(user);
        // Soft delete
        user.IsDeleted = true;
        user.DeletedAt = DateTime.UtcNow;
        user.DeletedBy = _userAccessor.UserName ?? "Unknown";

        
        // audit Log the after deletion
        await _auditService.LogAsync(
            action: "Delete",
            entityName: "ApplicationUser",
            entityId: user.Id.ToString(),
            changes: JsonSerializer.Serialize(deletedUserDto)
        );

        // return deleteResult.Succeeded;
        return true;
    }

    // // Helper method to map ApplicationUser + roles to UserDto
    // private UserDto MapToUserDto(ApplicationUser user, IList<string> roles)
    // {
    //     var userNameFromEmail = user.Email?.Split('@')[0] ?? string.Empty;
    //     return new UserDto
    //     {
    //         Id = user.Id,
    //         UserName = userNameFromEmail,
    //         Email = user.Email ?? string.Empty,
    //         Roles = roles.ToList(),
    //         TenantId = user.TenantId ?? Guid.Empty,
    //         // soft delete properties
    //         IsDeleted = user.IsDeleted,
    //         DeletedAt = user.DeletedAt,
    //         DeletedBy = user.DeletedBy
    //     };
    // }

}