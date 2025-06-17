using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MultiTenantTaskManager.Accessor;
using MultiTenantTaskManager.Authentication;
using MultiTenantTaskManager.DTOs.User;

namespace MultiTenantTaskManager.Services;

public class UserService : TenantAwareService, IUserService
{
    private readonly UserManager<ApplicationUser> _userManager;

    public UserService(
        UserManager<ApplicationUser> userManager,
        ClaimsPrincipal user,
        ITenantAccessor tenantAccessor,
        IAuthorizationService authorizationService
       ) : base(user, tenantAccessor, authorizationService)
    {
        _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
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
            userDtos.Add(MapToUserDto(user, roles));
        }

        return userDtos;
    }

    public async Task<UserDto> GetUserByIdAsync(string userId)
    {
        await AuthorizeSameTenantAsync();
        
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) throw new InvalidOperationException($"User with ID '{userId}' not found.");

        var roles = await _userManager.GetRolesAsync(user);
        return MapToUserDto(user, roles);
    }

    public async Task<UserDto> UpdateUserAsync(string userId, UpdateUserDto dto)
    {
        await AuthorizeSameTenantAsync();

        if (dto == null) throw new ArgumentNullException(nameof(dto));

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            throw new InvalidOperationException($"User with ID '{userId}' not found.");

        // Validate roles
        var validRoles = new[] {
            AppRoles.SuperAdmin,
            AppRoles.Admin,
            AppRoles.Manager,
            AppRoles.Member,
            AppRoles.SpecialMember
        };

        // Validate the provided role
        if (!validRoles.Contains(dto.Role))
            throw new InvalidOperationException($"Invalid role '{dto.Role}'. Allowed roles are: {string.Join(", ", validRoles)}");

        // Remove all existing roles
        var currentRoles = await _userManager.GetRolesAsync(user);
        var removeResult = await _userManager.RemoveFromRolesAsync(user, currentRoles);
        if (!removeResult.Succeeded)
            throw new InvalidOperationException($"Failed to remove existing roles from user '{userId}'.");

        // Add the new role
        var addResult = await _userManager.AddToRoleAsync(user, dto.Role);
        if (!addResult.Succeeded)
            throw new InvalidOperationException($"Failed to assign role '{dto.Role}' to user '{userId}'.");

        return MapToUserDto(user, new List<string> { dto.Role });
    }

    public async Task<bool> DeleteUserAsync(string userId)
    {
        await AuthorizeSameTenantAsync();

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return false;

        var deleteResult = await _userManager.DeleteAsync(user);
        return deleteResult.Succeeded;
    }

    // Helper method to map ApplicationUser + roles to UserDto
    private UserDto MapToUserDto(ApplicationUser user, IList<string> roles)
    {
        var userNameFromEmail = user.Email?.Split('@')[0] ?? string.Empty;
        return new UserDto
        {
            Id = user.Id,
            UserName = userNameFromEmail,
            Email = user.Email ?? string.Empty,
            Roles = roles.ToList(),
            TenantId = user.TenantId ?? Guid.Empty
        };
    }

}