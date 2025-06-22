using MultiTenantTaskManager.Authentication;
using MultiTenantTaskManager.Services;

namespace MultiTenantTaskManager.Models
{
    public class Tenant : ISoftDeletable
    {
        // Primary key
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = string.Empty;
        public string? Domain { get; set; } // optional: for subdomain-based resolution

        // Collection navigation property
        public ICollection<ApplicationUser> Users { get; set; } = new List<ApplicationUser>();
        public ICollection<Project> Projects { get; set; } = new List<Project>();

        public bool IsDeleted { get; set; }     // for soft deletion
        public DateTime? DeletedAt { get; set; }    // for soft deletion
        public string? DeletedBy { get; set; }  // for soft deletion
    }
}