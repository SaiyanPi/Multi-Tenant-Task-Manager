using System.ComponentModel.DataAnnotations;

namespace MultiTenantTaskManager.Authentication.DTOs;

public class RegisterDto
{
    // [EmailAddress]
    // [Required(ErrorMessage = "Email is required")]
    public string Email { get; set; } = string.Empty;

    // [Required(ErrorMessage = "Password is required")]
    public string Password { get; set; } = string.Empty;
    
    public string? Role { get; set; } // Optional, defaults to "Member"
    
    public Guid? TenantId { get; set; }
}