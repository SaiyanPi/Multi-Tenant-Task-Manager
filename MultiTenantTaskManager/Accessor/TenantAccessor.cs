namespace MultiTenantTaskManager.Accessor;

public class TenantAccessor : ITenantAccessor
{
    // public Guid TenantId { get; set; }
    
    private readonly IHttpContextAccessor _httpContextAccessor;
    private Guid? _tenantId;

    public TenantAccessor(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid TenantId
    {
        get
        {
            if (_tenantId.HasValue)
                return _tenantId.Value;

            var context = _httpContextAccessor.HttpContext;
            var user = context?.User;

            // If user is authenticated and is a SuperAdmin → no tenant scope
            if (user?.Identity?.IsAuthenticated == true &&
                user.IsInRole("SuperAdmin"))
            {
                throw new InvalidOperationException("SuperAdmin does not have a tenant context.");
            }

            // If request contains header → parse it
            var header = context?.Request.Headers["X-Tenant-ID"].FirstOrDefault();
            if (Guid.TryParse(header, out var tid))
            {
                _tenantId = tid;
                return tid;
            }

            // If user is unauthenticated and no tenant header exists, assume registration or setup
            if (user == null || user.Identity == null || !user.Identity.IsAuthenticated)
            {
                _tenantId = Guid.Empty;
                return Guid.Empty;
            }

            throw new InvalidOperationException("Tenant ID is missing or invalid in headers.");
        }
          set
        {
            _tenantId = value;
        }
    }
    public bool IsSuperAdmin =>
        _httpContextAccessor.HttpContext?.User?.IsInRole("SuperAdmin") == true;

}