using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using MultiTenantTaskManager.Authentication;
using MultiTenantTaskManager.DTOs;
using MultiTenantTaskManager.Models;
using MultiTenantTaskManager.Services;

namespace MultiTenantTaskManager.Controllers;

[Authorize(Policy = "canManageTenants")] 
[ApiController]
[Route("api/[controller]")]
[SkipTenantResolution] // allows skipping tenant resolution for this controller
public class TenantsController : ControllerBase
{
    private readonly ITenantService _tenantService;
    private readonly IAuthorizationService _authorizationService;

    public TenantsController(ITenantService tenantService, IAuthorizationService authorizationService)
    {
        _tenantService = tenantService ?? throw new ArgumentNullException(nameof(tenantService));
        _authorizationService = authorizationService ?? throw new ArgumentNullException(nameof(authorizationService));
    }
   
    // GET:/api/tenants
    [HttpGet]
    public async Task<ActionResult<IEnumerable<TenantDto>>> GetAllTenants()
    {
        var tenants = await _tenantService.GetAllTenantsAsync();

        // var authResult = await _authorizationService.AuthorizeAsync(User, null, "canViewAllTenants");
        // if (!authResult.Succeeded)
        // {
        //     return Forbid("You do not have permission to view all tenants.");
        // }

        return Ok(tenants);
    }

    // GET:/api/tenants/{id}
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TenantDto>> GetTenantById(Guid id)
    {
        var tenant = await _tenantService.GetTenantByIdAsync(id);
        if (tenant == null) return NotFound($"Tenant with ID {id} not found.");

        return Ok(tenant);
    }

    // POST:/api/tenants
    [HttpPost]
    public async Task<ActionResult<TenantDto>> CreateTenant([FromBody] CreateTenantDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var createdTenant = await _tenantService.CreateTenantAsync(dto);
        return CreatedAtAction(nameof(GetTenantById), new { id = createdTenant.Id }, createdTenant);
    }

    // PUT:/api/tenants/{id}
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<TenantDto>> UpdateTenant(Guid id, [FromBody] UpdateTenantDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        if(id != dto.Id) return BadRequest("Tenant ID in the URL does not match the ID in the body.");

        try
        {
            var updatedTenant = await _tenantService.UpdateTenantAsync(id, dto);
            return Ok(updatedTenant);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound($"Tenant with ID {id} not found. {ex.Message}");
        }
    }

    // DELETE:/api/tenants/{id}
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteTenant(Guid id)
    {
        var deleted = await _tenantService.DeleteTenantAsync(id);
        if (!deleted) return NotFound($"Tenant with ID {id} not found.");
        return NoContent();
    }
}