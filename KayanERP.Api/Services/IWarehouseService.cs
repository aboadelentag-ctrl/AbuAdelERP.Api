using KayanERP.Api.Models;
using KayanERP.Shared.DTOs;

namespace KayanERP.Api.Services;

public interface IWarehouseService
{
    Task<List<Warehouse>> GetAllWarehousesAsync();
    Task<Warehouse> AddWarehouseAsync(WarehouseCreateDto dto);
}