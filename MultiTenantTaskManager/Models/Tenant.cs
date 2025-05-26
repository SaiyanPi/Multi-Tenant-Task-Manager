using MultiTenantTaskManager.Authentication;

namespace MultiTenantTaskManager.Models
{
    public class Tenant
    {
        // Primary key
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = string.Empty;
        public string? Domain { get; set; } // optional: for subdomain-based resolution
        
        // Collection navigation property
        public ICollection<ApplicationUser> Users { get; set; } = new List<ApplicationUser>();
    }
}