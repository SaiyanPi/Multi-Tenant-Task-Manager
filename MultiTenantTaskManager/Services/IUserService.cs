

using MultiTenantTaskManager.DTOs;

namespace MultiTenantTaskManager.Services;
public interface IUserService
{
    Task<IEnumerable<UserDto>> GetAllUsersForTenantAsync(int page = 1, int pageSize = 10);
    Task<UserDto> GetUserByIdAsync(string userId);
    Task<UserDto> UpdateUserAsync(string userId, UpdateUserDto dto);
    Task<bool> DeleteUserAsync(string userId);
}