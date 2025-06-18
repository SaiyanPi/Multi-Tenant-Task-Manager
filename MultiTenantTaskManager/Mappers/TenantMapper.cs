using MultiTenantTaskManager.DTOs.Tenant;
using MultiTenantTaskManager.Models;

namespace  MultiTenantTaskManager.Mappers;

public static class TenantMapper
{
    public static TenantDto ToTenantDto(this Tenant tenant) => new TenantDto
    {
        Id = tenant.Id,
        Name = tenant.Name,
        Domain = tenant.Domain,
         Projects = tenant.Projects?.Select(p => p.ToProjectDto()).ToList() ?? new()
    };

    public static Tenant ToTenantModel(this CreateTenantDto dto) => new Tenant
    {
        Name = dto.Name,
        Domain = dto.Domain
    };

    public static void UpdateFromDto(this Tenant tenant, UpdateTenantDto dto)
    {
        tenant.Name = dto.Name;
        tenant.Domain = dto.Domain;
    }
}