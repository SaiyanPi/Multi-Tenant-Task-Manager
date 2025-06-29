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

            // Regular tenant-scoped user registration -----

            var tenantId = _tenantAccessor.TenantId;
            var requestBodyTenantId = model.TenantId;

            // if tenantId is not provided in request body and the tenantId from tenant accessor has empty value
            if (!requestBodyTenantId.HasValue && tenantId == Guid.Empty)
            {
                throw new InvalidOperationException("Tenant ID is required. Provide it in request header or body.");
            }

            // Check for mismatch if both are provided
            if (requestBodyTenantId.HasValue && tenantId != Guid.Empty && requestBodyTenantId.Value != tenantId)
                throw new InvalidOperationException("Tenant ID in request body does not match the tenant context in header.");

            // Set final TenantId: prefer body if present, otherwise use header
            model.TenantId ??= tenantId;

            var tenantExists = await _dbContext.Tenants.AnyAsync(t => t.Id == model.TenantId);
            if (!tenantExists)
            {
                return BadRequest("Tenant does not exist.");
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

            // if soft-deleted user exists then reuse soft-deleted user instead of creating
            var normalizedEmail = model.Email.Trim().ToLowerInvariant();
            var userName = $"{normalizedEmail}_{tenantId}";

            var existingUser = await _userManager.Users
                .IgnoreQueryFilters() // to include soft-deleted users
                .FirstOrDefaultAsync(u => u.UserName == userName && u.IsDeleted);

            if (existingUser != null)
            {
                existingUser.IsDeleted = false;
                await _userManager.UpdateAsync(existingUser);
                return Ok("User registered successfully.");
            }

            // Create a new user object
            var user = new ApplicationUser
            {
                // UserName = model.Email,
                UserName = userName, // ensures UserName is unique globally, while still showing the same email to users.
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
        
        // First, try to find user by email (without tenant filter)
        var userByEmail = await _userManager.Users
            .Where(u => u.Email == model.Email)
            .ToListAsync();

         // If no user found at all
        if (userByEmail.Count == 0)
            return Unauthorized(new { message = "Email not registered." });

        // Try to find if any user with this email is SuperAdmin
        var superAdminUser = userByEmail.FirstOrDefault(u => 
            _userManager.GetRolesAsync(u).Result.Contains(AppRoles.SuperAdmin));

        if (superAdminUser != null)
        {
            // We found a SuperAdmin user, so login that user (only one superadmin per email should exist)
            var isPasswordValid = await _userManager.CheckPasswordAsync(superAdminUser, model.Password);
            if (!isPasswordValid)
                return Unauthorized(new { message = "Invalid email or password." });

            var token = GenerateJwtToken(superAdminUser);
            return Ok(new { token });
        }

        // Not a SuperAdmin, get tenant from header
        Guid currentTenantId;
        try
        {
            currentTenantId = _tenantAccessor.TenantId;
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }

        // Now find user in this tenant
        var user = userByEmail.FirstOrDefault(u => u.TenantId == currentTenantId);

        if (user == null)
            return Unauthorized(new { message = "Tenant mismatch: Check your tenant and email " });

        var isPasswordValidRegular = await _userManager.CheckPasswordAsync(user, model.Password);
        if (!isPasswordValidRegular)
            return Unauthorized(new { message = "Invalid email or password." });

        var userRoles = await _userManager.GetRolesAsync(user);
        if (!userRoles.Contains(AppRoles.SuperAdmin) && user.TenantId != currentTenantId)
            return Forbid("Tenant mismatch: You are not authorized to log in to this tenant.");

        var tokenRegular = GenerateJwtToken(user);
        return Ok(new { token = tokenRegular });
       
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