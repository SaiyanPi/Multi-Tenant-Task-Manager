using Microsoft.AspNetCore.Identity;
using MultiTenantTaskManager.Models;
using MultiTenantTaskManager.Services;

namespace MultiTenantTaskManager.Authentication;

public class ApplicationUser : IdentityUser
{
    // foreign key to the Tenant
    public Guid? TenantId { get; set; } //nullable because SuperAdmin user does not belong to any tenant

    // Reference navigation property to Tenant
    public Tenant? Tenant { get; set; }


    // public bool IsDeleted { get; set; }     // for soft deletion
    // public DateTime? DeletedAt { get; set; }    // for soft deletion
    // public string? DeletedBy { get; set; }  // for soft deletion
}