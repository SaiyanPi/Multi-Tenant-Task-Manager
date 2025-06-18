using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MultiTenantTaskManager.Authentication;
using MultiTenantTaskManager.Models;

namespace MultiTenantTaskManager.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
: IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<Tenant> Tenants { get; set; }
    public DbSet<Project> Projects { get; set; }
    public DbSet<TaskItem> TaskItems { get; set; }
    public DbSet<AuditLog> AuditLogs { get; set; } // For tracking changes

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        // configuring relationships

        // Tenant → Users (Required, Restrict)
        builder.Entity<Tenant>()
            .HasMany(t => t.Users)
            .WithOne(u => u.Tenant)
            .HasForeignKey(u => u.TenantId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);
        //.OnDelete(DeleteBehavior.Cascade); // Optional: cascade user deletion with tenant;

        // Tenant → Projects (Required, Cascade)
        builder.Entity<Tenant>()
            .HasMany(t => t.Projects)
            .WithOne(p => p.Tenant)
            .HasForeignKey(p => p.TenantId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade); // Deletes all projects when tenant is deleted

        // Project → Tasks (Required, Cascade)
        builder.Entity<Project>()
            .HasMany(p => p.Tasks)
            .WithOne(t => t.Project)
            .HasForeignKey(t => t.ProjectId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade); // Deletes all tasks when project is deleted

        // TaskItem → Tenant (Required, Restrict)
        builder.Entity<TaskItem>()
            .HasOne(t => t.Tenant)
            .WithMany()
            .HasForeignKey(t => t.TenantId)
            .OnDelete(DeleteBehavior.Restrict); // this prevents cascading deletes from Tenant to TaskItem
                                                // directly because we already have cascade delete from Tenant->Project->TaskItem
    }
    
}