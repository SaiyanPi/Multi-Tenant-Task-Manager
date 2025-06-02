namespace MultiTenantTaskManager;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class SkipTenantResolutionAttribute : Attribute
{
    // This attribute can be used to mark controllers or actions that should skip tenant resolution
    // For example, you can use it like this:
    // [SkipTenantResolution]
    // public class TenantController : ControllerBase
    // {
    //     ...
    // }
}