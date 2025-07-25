using System.Linq.Expressions;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MultiTenantTaskManager.Authentication;
using MultiTenantTaskManager.Models;
using MultiTenantTaskManager.Services;

namespace MultiTenantTaskManager.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
: IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<Tenant> Tenants { get; set; }
    public DbSet<Project> Projects { get; set; }
    public DbSet<TaskItem> TaskItems { get; set; }
    public DbSet<AuditLog> AuditLogs { get; set; } // For tracking changes
    public DbSet<Notification> Notifications { get; set; }
    public DbSet<Comment> Comments { get; set; } // For comments on tasks and projects

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        // configuring relationships

        // Tenant → Users (Required, Restrict)
        builder.Entity<Tenant>()
            .HasMany(t => t.Users)
            .WithOne(u => u.Tenant)
            .HasForeignKey(u => u.TenantId)
            // .IsRequired()    // doesn't require for super admin user
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
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict); // this prevents cascading deletes from Tenant to TaskItem
                                                // directly because we already have cascade delete from Tenant->Project->TaskItem

        // one to many relationship: Project → ApplicationUsers (AssignedUsers)
        builder.Entity<ApplicationUser>()
            .HasOne(u => u.Project)
            .WithMany(p => p.AssignedUsers)
            .HasForeignKey(u => u.ProjectId)
            .OnDelete(DeleteBehavior.SetNull); // when project is deleted, clear reference/set ProjectId field to NULL

        // one to one relationship: TaskItem → ApplicationUser (AssignedUser)
        builder.Entity<TaskItem>()
            .HasOne(t => t.AssignedUser)
            .WithOne(u => u.AssignedTask)
            .HasForeignKey<TaskItem>(t => t.AssignedUserId)
            .OnDelete(DeleteBehavior.SetNull); // optional


        // Comment -> Project
        builder.Entity<Comment>()
            .HasOne(c => c.Project)
            .WithMany(p => p.Comments)
            .HasForeignKey(c => c.ProjectId)
            .OnDelete(DeleteBehavior.SetNull); 

        // Comment -> TaskItem
        builder.Entity<Comment>()
            .HasOne(c => c.TaskItem)
            .WithMany(t => t.Comments)
            .HasForeignKey(c => c.TaskItemId)
            .OnDelete(DeleteBehavior.SetNull);

        // Comment -> User(Author)
        builder.Entity<Comment>()
            .HasOne(c => c.User)
            .WithMany()
            .HasForeignKey(c => c.UserId)
            .OnDelete(DeleteBehavior.SetNull); 

        // Comment Properties
        builder.Entity<Comment>()
            .Property(c => c.Content)
            .IsRequired()
            .HasMaxLength(1000);

        // ----------------------------------------------------------------------------------------------------

        // Adding a global query filter for soft deletion
        // This ensures IsDeleted = false by default for all queries unless explicitly overridden

        foreach (var entityType in builder.Model.GetEntityTypes())
        {
            var clrType = entityType.ClrType;
            if (typeof(ISoftDeletable).IsAssignableFrom(clrType))
            {
                var parameter = Expression.Parameter(clrType, "e");
                var propertyMethod = typeof(EF).GetMethod(nameof(EF.Property))?
                    .MakeGenericMethod(typeof(bool));

                var isDeletedProperty = Expression.Call(
                    propertyMethod!,
                    parameter,
                    Expression.Constant(nameof(ISoftDeletable.IsDeleted))
                );

                var compareExpression = Expression.Equal(isDeletedProperty, Expression.Constant(false));
                var lambda = Expression.Lambda(compareExpression, parameter);

                // ignores soft-deleted records by default using global query filters
                builder.Entity(clrType).HasQueryFilter(lambda);
                // eg, builder.Entity<TaskItem>().HasQueryFilter(t => !t.IsDeleted) 
            }
        }
    }
    
    // Override the SaveChangesAsync to automatically set the current date and time in the 'CreatedAt' properties
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var utcNow = DateTime.UtcNow;

        // single entity
        // foreach (var entry in ChangeTracker.Entries<TaskItem>())
        // {
        //     if (entry.State == EntityState.Added)
        //     {
        //         entry.Entity.CreatedAt = utcNow;
        //     }

        //     // Optional: Handle UpdatedAt/CompletedAt if needed here
        // }


        // globally
        foreach (var entry in ChangeTracker.Entries<IAuditable>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = utcNow;
            }
        }

        return await base.SaveChangesAsync(cancellationToken);
    }

}

