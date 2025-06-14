using MultiTenantTaskManager.DTOs;
using MultiTenantTaskManager.Models;

namespace  MultiTenantTaskManager.Mappers;

public static class TenantMapper
{
    public static TenantDto ToDto(this Tenant tenant) => new TenantDto
    {
        Id = tenant.Id,
        Name = tenant.Name,
        Domain = tenant.Domain
    };

    public static Tenant ToModel(this CreateTenantDto dto) => new Tenant
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