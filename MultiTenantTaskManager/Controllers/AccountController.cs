using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using MultiTenantTaskManager.Accessor;
using MultiTenantTaskManager.Authentication;
using MultiTenantTaskManager.Authentication.DTOs;

namespace MultiTenantTaskManager.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AccountController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IConfiguration _configuration;
    private readonly ITenantAccessor _tenantAccessor;
    private readonly RoleManager<IdentityRole> _roleManager;

    public AccountController(UserManager<ApplicationUser> userManager, IConfiguration configuration,
        ITenantAccessor tenantAccessor, RoleManager<IdentityRole> roleManager)
    {
        _userManager = userManager;
        _configuration = configuration;
        _tenantAccessor = tenantAccessor;
        _roleManager = roleManager;
    }

    [HttpPost("register-admin")]
    public Task<IActionResult> RegisterAdmin([FromBody] RegisterDto model)
    {
        return RegisterUserWithRole(model, AppRoles.Admin);
    }

    [HttpPost("register-manager")]
    public Task<IActionResult> RegisterVip([FromBody] RegisterDto model)
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
    // [SkipTenantResolution] 
    public async Task<IActionResult> RegisterUserWithRole([FromBody] RegisterDto model, string roleName)
    {
        if (ModelState.IsValid)
        {
            model.TenantId = _tenantAccessor.TenantId;
            var existedUser = await _userManager.FindByNameAsync(model.Email);

            if (existedUser != null)
            {
                ModelState.AddModelError("", "Email already exists!");
                return BadRequest(ModelState);
            }
            // Create a new user object
            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                TenantId = model.TenantId,
                SecurityStamp = Guid.NewGuid().ToString()
            };
            // Try to save the user
            var userResult = await _userManager.CreateAsync(user, model.Password);
            // Add the user to the role
            var roleResult = await _userManager.AddToRoleAsync(user, roleName);

            if (userResult.Succeeded && roleResult.Succeeded)
            {
                // Store custom claims in the AspNetUserClaims table
                var claimsToAdd = new List<Claim>
                {
                    // Every user gets a tenant_id claim
                    new("tenant_id", user.TenantId.ToString())
                };

                // Based on role, assign relevant claim permissions
                if (roleName == AppRoles.Admin)
                {
                    claimsToAdd.Add(new Claim(AppClaimTypes.can_create_delete_tenant, "true"));
                    claimsToAdd.Add(new Claim(AppClaimTypes.can_create_delete_project, "true"));
                    claimsToAdd.Add(new Claim(AppClaimTypes.can_create_delete_task, "true"));
                }
                else if (roleName == AppRoles.Manager)
                {
                    claimsToAdd.Add(new Claim(AppClaimTypes.can_create_delete_project, "true"));
                    claimsToAdd.Add(new Claim(AppClaimTypes.can_create_delete_task, "true"));
                }
                else if (roleName == AppRoles.SpecialMember)
                {
                    claimsToAdd.Add(new Claim(AppClaimTypes.can_create_delete_task, "true"));
                }
                else if (roleName == AppRoles.Member)
                {
                    claimsToAdd.Add(new Claim(AppClaimTypes.can_create_delete_task, "false"));
                }

                foreach (var claim in claimsToAdd)
                {
                    await _userManager.AddClaimAsync(user, claim);
                }

                // finally response
                return Ok("User registered successfully.");
            }

            foreach (var error in userResult.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }

        }

        return BadRequest(ModelState);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto model)
    {
        if (ModelState.IsValid)
        {
            
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user != null)
            {
                if (await _userManager.CheckPasswordAsync(user, model.Password))
                {
                    var token = GenerateJwtToken(user);
                    return Ok(new { token });
                }
            }
        // If the user is not found, display an error message
        ModelState.AddModelError("", "Invalid username or password");
        }

        return BadRequest(ModelState);
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
            // new(JwtRegisteredClaimNames.Sub, user.Id),
            // new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            // new("tenant_id", user.TenantId.ToString())
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Email, user.Email ?? string.Empty),
            new("tenant_id", user.TenantId.ToString())  // custom tenant claim
        };

        // 2. Add role claims
        var userRoles = await _userManager.GetRolesAsync(user);
        claims.AddRange(userRoles.Select(role => new Claim(ClaimTypes.Role, role)));

        // 3. Add custom claims stored in DB
        var dbClaims = await _userManager.GetClaimsAsync(user);
        claims.AddRange(dbClaims.Where(c => 
            c.Type != ClaimTypes.Role && 
            c.Type != ClaimTypes.Email && 
            c.Type != ClaimTypes.NameIdentifier && 
            c.Type != "tenant_id")); // Avoid duplication

        // 4. Sign and issue token
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