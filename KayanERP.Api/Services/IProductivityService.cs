using System.Threading.Tasks;
using KayanERP.Shared.DTOs;

namespace KayanERP.Api.Services
{
    public interface IProductivityService
    {
        Task<object> AddDepartmentAsync(DepartmentCreateDto dto);
        Task<object> AddEmployeeAsync(EmployeeCreateDto dto);
        Task<object> GetEmployeeProductivityAsync(long employeeId);
        Task<byte[]> ExportEmployeeProductivityToExcelAsync(long employeeId);
    }
}