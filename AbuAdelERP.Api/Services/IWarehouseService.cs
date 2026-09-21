using AbuAdelERP.Api.Models;
using AbuAdelERP.Api.DTOs;

namespace AbuAdelERP.Api.Services;

public interface IWarehouseService
{
    Task<List<Warehouse>> GetAllWarehousesAsync();
    Task<Warehouse> AddWarehouseAsync(WarehouseCreateDto dto);
}