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

    // user assignment to a task
    public TaskItem? AssignedTask { get; set; } // reference navigation property to a TaskItem

    
    // user assignment to a project
    public int? ProjectId { get; set; } // foreign key from Project
    public Project? Project { get; set; } // reference navigation property to a Project

    public string? RoleInProject { get; set; } // AppRoles.Member or AppRoles.SpecialMember pr AppRoles.Manager


    // for soft deletion
    public bool IsDeleted { get; set; }    
    public DateTime? DeletedAt { get; set; }  
    public string? DeletedBy { get; set; } 
}