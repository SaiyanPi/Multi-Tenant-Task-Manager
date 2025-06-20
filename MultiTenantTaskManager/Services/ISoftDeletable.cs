namespace MultiTenantTaskManager.Services;

public interface ISoftDeletable
{
    bool IsDeleted { get; set; }
    DateTime? DeletedAt { get; set; }
    string DeletedBy { get; set; }
    // Guid? DeletedByUserId { get; set; }
}