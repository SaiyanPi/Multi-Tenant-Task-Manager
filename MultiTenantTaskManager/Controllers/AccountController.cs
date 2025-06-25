using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using MultiTenantTaskManager.Accessor;
using MultiTenantTaskManager.Authentication;
using MultiTenantTaskManager.Authentication.DTOs;
using MultiTenantTaskManager.Data;

namespace MultiTenantTaskManager.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AccountController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IConfiguration _configuration;
    private readonly ITenantAccessor _tenantAccessor;
    private readonly RoleManager<IdentityRole> _roleManager;

    public AccountController(ApplicationDbContext dbContext, UserManager<ApplicationUser> userManager,
        IConfiguration configuration, ITenantAccessor tenantAccessor, RoleManager<IdentityRole> roleManager)
    {
        _dbContext = dbContext;
        _userManager = userManager;
        _configuration = configuration;
        _tenantAccessor = tenantAccessor;
        _roleManager = roleManager;
    }

    [HttpPost("register-superAdmin")]
    [SkipTenantResolution] 
    public Task<IActionResult> RegisterSuperAdmin([FromBody] RegisterDto model)
    {
        return RegisterUserWithRole(model, AppRoles.SuperAdmin);
    }

    [HttpPost("register-admin")]
    public Task<IActionResult> RegisterAdmin([FromBody] RegisterDto model)
    {
        return RegisterUserWithRole(model, AppRoles.Admin);
    }

    [HttpPost("register-manager")]
    public Task<IActionResult> RegisterManager([FromBody] RegisterDto model)
    {
        return RegisterUserWithRole(model, AppRoles.Manager);
    }

    [HttpPost("register-specialMember")]
    public Task<IActionResult> RegisterSpecialMember([FromBody] RegisterDto model)
    {
        return RegisterUserWithRole(model, AppRoles.SpecialMember);
    }

    [HttpPost("register")]
    public Task<IActionResult> Register([FromBody] RegisterDto model)
    {
        return RegisterUserWithRole(model, AppRoles.Member);
    }

    // [HttpPost("register")]
    public async Task<IActionResult> RegisterUserWithRole([FromBody] RegisterDto model, string roleName)
    {
        if (ModelState.IsValid)
        {
            // special handling for super admin outside tenant scope (to set tenantId null)
            if (roleName == AppRoles.SuperAdmin)
            {
                var existedSuperAdmin = await _userManager.FindByEmailAsync(model.Email);
                if (existedSuperAdmin != null)
                {
                    ModelState.AddModelError("", "Email already exists!");
                    return BadRequest(ModelState);
                }

                var SuperAdmin = new ApplicationUser
                {
                    UserName = model.Email,
                    Email = model.Email,
                    TenantId = null, // SuperAdmin lives outside tenant scope
                    SecurityStamp = Guid.NewGuid().ToString()
                };

                var superAdminResult = await _userManager.CreateAsync(SuperAdmin, model.Password);
                var superRoleResult = await _userManager.AddToRoleAsync(SuperAdmin, AppRoles.SuperAdmin);

                if (superAdminResult.Succeeded && superRoleResult.Succeeded)
                {
                    var claimsToAdd = new List<Claim>
                    {
                        new Claim(AppClaimTypes.can_manage_tenants, "true")
                    };

                    foreach (var claim in claimsToAdd)
                    {
                        await _userManager.AddClaimAsync(SuperAdmin, claim);
                    }

                    return Ok("Super admin registered successfully.");
                }

                foreach (var error in superAdminResult.Errors)
                    ModelState.AddModelError("", error.Description);

                return BadRequest(ModelState);

            }

            // Regular tenant-scoped user registration

            // if provided TenantId does not match with the header TenantId, return an error
            if (model.TenantId != _tenantAccessor.TenantId)
            {
                return BadRequest("TenantId in the request body does not match the current tenant context.");
            }

            // Fallback to tenant accessor if TenantId is not provided in body
            if (!model.TenantId.HasValue || model.TenantId == Guid.Empty)
            {
                model.TenantId = _tenantAccessor.TenantId;
            }

            // If still missing, return an error
            if (!model.TenantId.HasValue || model.TenantId == Guid.Empty)
            {
                return BadRequest("TenantId is required either in the request body or header.");
            }

            var tenantExists = await _dbContext.Tenants.AnyAsync(t => t.Id == model.TenantId);
            if (!tenantExists)
            {
                return BadRequest("Tenant does not exist.");
            }

            if (model.TenantId != _tenantAccessor.TenantId)
            {
                return BadRequest("TenantId in the request body does not match the current tenant context.");
            }

            // following code is commented because even if we maintain user uniqueness per tenant
            // the default UserManager logic enforces uniqueness of UserName globally, regardless of the TenantId.
            // user uniqueness is ensured from custom MultiTenantUserValidator.cs

            // -------------------------------------------------------------------
            //// unique user: this prevents duplicate user globally but we don't want user uniqueness globally but
            //// rather per tenant because user from another tenant can have the same name hence possibly email
            //// resulting in unable to register the user.

            // var existedUser = await _userManager.FindByNameAsync(model.Email);

            //// check if the user already exists in the current tenant not globally
            // var existedUser = await _userManager.Users
            //     .FirstOrDefaultAsync(u =>
            //         u.Email == model.Email &&
            //         u.TenantId == _tenantAccessor.TenantId);

            // if (existedUser != null)
            // {
            //     ModelState.AddModelError("", "Email already exists!");
            //     return BadRequest(ModelState);
            // }
            // ----------------------------------------------------------------------

            // Create a new user object
            var user = new ApplicationUser
            {
                // UserName = model.Email,
                UserName = $"{model.Email}_{model.TenantId}", // ensures UserName is unique globally, while still showing the same email to users.
                Email = model.Email,
                TenantId = model.TenantId,
                SecurityStamp = Guid.NewGuid().ToString()
            };
            // Try to save the user
            var userResult = await _userManager.CreateAsync(user, model.Password); // automatically applies UserValidator<TUser> which is currently replaced by MultiTenantUserValidator
            // Add the user to the role
            var roleResult = await _userManager.AddToRoleAsync(user, roleName);

            if (userResult.Succeeded && roleResult.Succeeded)
            {
                // Store custom claims in the AspNetUserClaims table
                var claimsToAdd = new List<Claim>
                {
                    // Every user gets a tenant_id claim
                    new("tenant_id", user.TenantId?.ToString() ?? string.Empty)
                };

                // following code is actually unnecessary because there is no need to store role claims in
                // the database and GenerateJwtToken method already adds claims dynamically based on roles

                // // Based on role, assign relevant claim permissions
                // if (roleName == AppRoles.Admin)
                // {
                //     claimsToAdd.Add(new Claim(AppClaimTypes.can_manage_tenants, "true"));
                //     claimsToAdd.Add(new Claim(AppClaimTypes.can_manage_users, "true"));
                //     claimsToAdd.Add(new Claim(AppClaimTypes.can_assign_tasks, "true"));
                //     claimsToAdd.Add(new Claim(AppClaimTypes.can_edit_projects, "true"));
                // }
                // else if (roleName == AppRoles.Manager)
                // {
                //     claimsToAdd.Add(new Claim(AppClaimTypes.can_assign_tasks, "true"));
                //     claimsToAdd.Add(new Claim(AppClaimTypes.can_edit_projects, "true"));
                //     claimsToAdd.Add(new Claim(AppClaimTypes.can_edit_tasks, "true"));
                //     claimsToAdd.Add(new Claim(AppClaimTypes.can_view_tasks, "true"));
                // }
                // else if (roleName == AppRoles.SpecialMember)
                // {
                //     claimsToAdd.Add(new Claim(AppClaimTypes.can_edit_tasks, "true"));
                //     claimsToAdd.Add(new Claim(AppClaimTypes.can_view_tasks, "true"));
                // }
                // else if (roleName == AppRoles.Member)
                // {
                //     claimsToAdd.Add(new Claim(AppClaimTypes.can_view_tasks, "true"));
                // }

                // foreach (var claim in claimsToAdd)
                // {
                //     await _userManager.AddClaimAsync(user, claim);
                // }

                return Ok("User registered successfully.");
            }

            // foreach (var error in userResult.Errors)
            // {
            //     ModelState.AddModelError("", error.Description);
            // }
            
            // Ignoring the validation error related to duplicate username when registering because we only
            // want to show email/password related validation error
            var filteredErrors = userResult.Errors
                .Where(e => !e.Code.Contains("DuplicateUserName", StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (filteredErrors.Count > 0)
            {
                foreach (var error in filteredErrors)
                {
                    ModelState.AddModelError("", error.Description);
                }
            }

        }

        return BadRequest(ModelState);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto model)
    {
        if (!ModelState.IsValid)
            return BadRequest(new { message = "Invalid request data.", errors = ModelState });
        
        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user == null)
            return Unauthorized(new { message = "Invalid email or password." });

        var isPasswordValid = await _userManager.CheckPasswordAsync(user, model.Password);
        if (!isPasswordValid)
            return Unauthorized(new { message = "Invalid email or password." });
        
        var userRoles = await _userManager.GetRolesAsync(user);
        // If user is SuperAdmin, skip tenant checks
        if (!userRoles.Contains(AppRoles.SuperAdmin))
        {
            // Read tenant ID from header
            var headerTenantId = HttpContext.Request.Headers["X-Tenant-ID"].ToString();
            if (string.IsNullOrWhiteSpace(headerTenantId))
                return BadRequest(new { message = "Tenant ID is required in the 'X-Tenant-ID' header." });

            // Validate tenant match
            var userTenantId = user.TenantId.ToString();
            if (!string.Equals(userTenantId, headerTenantId, StringComparison.OrdinalIgnoreCase))
                return Forbid("Tenant mismatch: You are not authorized to log in to this tenant.");

        }

        var token = GenerateJwtToken(user);
        return Ok(new { token });
    }

    private async Task<string?> GenerateJwtToken(ApplicationUser user)
    {
        var secret = _configuration["JwtConfig:Secret"];
        var issuer = _configuration["JwtConfig:ValidIssuer"];
        var audience = _configuration["JwtConfig:ValidAudiences"];
        if (secret is null || issuer is null || audience is null)
        {
            throw new ApplicationException("Jwt is not set in the configuration");
        }

        // 1. Get built-in claims
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Email, user.Email ?? string.Empty),
            
            // UserName claim added for Auditing and Logging
            new(ClaimTypes.Name, user.UserName ?? string.Empty),
            
            // For Tenant-Specific Access: include the user's tenant ID in the JWT token
            new("tenant_id", user.TenantId?.ToString() ?? string.Empty) 
        };

        // 2. Add role claims
        var userRoles = await _userManager.GetRolesAsync(user);
        claims.AddRange(userRoles.Select(role => new Claim(ClaimTypes.Role, role)));

        // Dynamically add claims based on roles
        foreach (var role in userRoles)
        {
            switch (role)
            {
                case AppRoles.SuperAdmin:
                    claims.Add(new Claim(AppClaimTypes.can_manage_tenants, "true"));
                    break;

                case AppRoles.Admin:
                    claims.Add(new Claim(AppClaimTypes.can_manage_tenant, "true"));
                    claims.Add(new Claim(AppClaimTypes.can_manage_users, "true"));
                    //claims.Add(new Claim(AppClaimTypes.can_assign_tasks, "true"));
                    claims.Add(new Claim(AppClaimTypes.can_manage_projects, "true"));
                    break;

                case AppRoles.Manager:
                    //claims.Add(new Claim(AppClaimTypes.can_assign_tasks, "true"));
                    claims.Add(new Claim(AppClaimTypes.can_manage_projects, "true"));
                    claims.Add(new Claim(AppClaimTypes.can_manage_tasks, "true"));
                    claims.Add(new Claim(AppClaimTypes.can_view_tasks, "true"));
                    break;

                case AppRoles.SpecialMember:
                    claims.Add(new Claim(AppClaimTypes.can_manage_tasks, "true"));
                    claims.Add(new Claim(AppClaimTypes.can_view_tasks, "true"));
                    break;

                 case AppRoles.Member:
                    claims.Add(new Claim(AppClaimTypes.can_view_tasks, "true"));
                    break;
            }
        }

        // 3. Sign and issue token
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer,
            audience,
            claims.ToArray(),
            expires: DateTime.Now.AddDays(7),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}