using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using MultiTenantTaskManager.Models;
using MultiTenantTaskManager.Services;

namespace MultiTenantTaskManager.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TenantController : ControllerBase
{
    private readonly ITenantService _tenantService;
    public TenantController(ITenantService tenantService)
    {
        _tenantService = tenantService ?? throw new ArgumentNullException(nameof(tenantService));
    }

    // GET:/api/tenant
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Tenant>>> GetAllTenants()
    {
        var tenants = await _tenantService.GetAllTenantsAsync();

        return Ok(tenants);
    }

    // GET:/api/tenant/{id}
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<Tenant>> GetTenantById(Guid id)
    {
        var tenant = await _tenantService.GetTenantByIdAsync(id);
        if (tenant == null) return NotFound($"Tenant with ID {id} not found.");

        return Ok(tenant);
    }

    // POST:/api/tenant
    [HttpPost]
    public async Task<ActionResult<Tenant>> CreateTenant([FromBody] Tenant tenant)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var createdTenant = await _tenantService.CreateTenantAsync(tenant);
        return CreatedAtAction(nameof(GetTenantById), new { id = createdTenant.Id }, createdTenant);
    }

    // PUT:/api/tenant/{id}
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<Tenant>> UpdateTenant(Guid id, [FromBody] Tenant tenant)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        if(id != tenant.Id) return BadRequest("Tenant ID in the URL does not match the ID in the body.");

        try
        {
            var updatedTenant = await _tenantService.UpdateTenantAsync(id, tenant);
            return Ok(updatedTenant);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound($"Tenant with ID {id} not found. {ex.Message}");
        }
    }

    // DELETE:/api/tenant/{id}
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteTenant(Guid id)
    {
        var deleted = await _tenantService.DeleteTenantAsync(id);
        if (!deleted) return NotFound($"Tenant with ID {id} not found.");
        return NoContent();
    }
}