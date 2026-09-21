using System.Threading.Tasks;
using AbuAdelERP.Api.DTOs;

namespace AbuAdelERP.Api.Services
{
    public interface IProductivityService
    {
        Task<object> AddDepartmentAsync(DepartmentCreateDto dto);
        Task<object> AddEmployeeAsync(EmployeeCreateDto dto);
        Task<object> GetEmployeeProductivityAsync(long employeeId);
        Task<byte[]> ExportEmployeeProductivityToExcelAsync(long employeeId);
    }
}