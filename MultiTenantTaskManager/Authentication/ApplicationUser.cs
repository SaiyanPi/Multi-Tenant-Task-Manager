using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using MultiTenantTaskManager.Models;
using MultiTenantTaskManager.Services;

namespace MultiTenantTaskManager.Authentication;

public class ApplicationUser : IdentityUser, ISoftDeletable
{
    // foreign key to the Tenant
    public Guid? TenantId { get; set; } //nullable because SuperAdmin user does not belong to any tenant

    // Reference navigation property to Tenant
    public Tenant? Tenant { get; set; }
    
    
    // One-to-many project-user assignment
    public int? ProjectId { get; set; } // nullable for unassigned users
    public Project? Project { get; set; } // navigation
    public string? RoleInProject { get; set; } // AppRoles.Member or AppRoles.SpecialMember


    // for soft deletion
    public bool IsDeleted { get; set; }    
    public DateTime? DeletedAt { get; set; }  
    public string? DeletedBy { get; set; } 
}