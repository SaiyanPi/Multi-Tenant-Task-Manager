using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MultiTenantTaskManager.Authentication;
using MultiTenantTaskManager.DTOs.User;
using MultiTenantTaskManager.Services;

namespace MultiTenantTaskManager.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;
    public UsersController(IUserService userService)
    {
        _userService = userService ?? throw new ArgumentNullException(nameof(userService));
    }

    // GET:/api/users
    [Authorize(Policy = "canManageUsers")]
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ApplicationUser>>> GetAllUsers()
    {
        var users = await _userService.GetAllUsersForTenantAsync();
        return Ok(users);
    }

    // GET:/api/users/{id}
    [Authorize(Policy = "canManageUsers")]
    [HttpGet("{id}")]
    public async Task<ActionResult<ApplicationUser>> GetUserById(string id)
    {
        var user = await _userService.GetUserByIdAsync(id);
        if (user == null) return NotFound($"User with ID {id} not found.");
        return Ok(user);
    }

    // PUT:/api/users/{id}
    [Authorize(Policy = "canManageUsers")]
    [HttpPut("{id}")]
    public async Task<ActionResult<ApplicationUser>> UpdateUser(string id, [FromBody] UpdateUserDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        try
        {
            var updatedUser = await _userService.UpdateUserAsync(id, dto);
            return Ok(updatedUser);
        }
        catch (InvalidOperationException ex)
        {
            // Return 409 Conflict with a user-friendly message
            return Conflict(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            // Catch other unexpected errors (optional but good practice)
            return StatusCode(500, new { message = "An unexpected error occurred.", details = ex.Message });
        }
    }

    // DELETE:/api/users/{id}
    [Authorize(Policy = "canManageUsers")]
    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteUser(string id)
    {
        try
        {
            var result = await _userService.DeleteUserAsync(id);
            if (!result) return NotFound($"User with ID {id} not found.");
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            // Return 409 Conflict with a user-friendly message
            return Conflict(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            // Catch other unexpected errors (optional but good practice)
            return StatusCode(500, new { message = "An unexpected error occurred.", details = ex.Message });
        }
    }
}