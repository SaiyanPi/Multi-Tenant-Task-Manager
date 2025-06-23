using MultiTenantTaskManager.Authentication;
using MultiTenantTaskManager.DTOs.User;

namespace MultiTenantTaskManager.Mappers;

public static class UserMapper
{
    public static UserDto ToUserDto(ApplicationUser user, IList<string> roles)
    {
        var userNameFromEmail = user.Email?.Split('@')[0] ?? string.Empty;

        return new UserDto
        {
            Id = user.Id,
            UserName = userNameFromEmail,
            Email = user.Email ?? string.Empty,
            Roles = roles.ToList(),
            TenantId = user.TenantId ?? Guid.Empty,
            // IsDeleted = user.IsDeleted,
            // DeletedAt = user.DeletedAt,
            // DeletedBy = user.DeletedBy
        };
    }
}