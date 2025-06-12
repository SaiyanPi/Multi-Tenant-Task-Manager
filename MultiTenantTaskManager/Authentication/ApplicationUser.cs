using Microsoft.AspNetCore.Identity;
using MultiTenantTaskManager.Models;

namespace MultiTenantTaskManager.Authentication;

public class ApplicationUser : IdentityUser
{
    // foreign key to the Tenant
    public Guid? TenantId { get; set; }

    // Reference navigation property to Tenant
    public Tenant? Tenant { get; set; }
}