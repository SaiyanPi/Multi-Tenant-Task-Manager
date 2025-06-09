namespace MultiTenantTaskManager.Authentication;
public static class AppAuthorizationPolicies
{
    public const string Require_can_create_delete_tenant = "Require_can_create_delete_tenant";
    public const string Require_can_create_delete_project = "Require_can_create_delete_project";
    public const string Require_can_create_delete_task = "Require_can_create_delete_task";
    public const string Require_can_create_delete_project_task = "Require_can_create_delete_project_task";
}