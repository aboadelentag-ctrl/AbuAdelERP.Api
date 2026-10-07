using System.Collections.Generic;
using System.Threading.Tasks;
using AbuAdelERP.Api.DTOs;

namespace AbuAdelERP.Api.Services
{
    public interface IQualityService
    {
        Task<object> SubmitInspectionAsync(QualityInspectionCreateDto dto);
        Task<List<QualityInspectionViewDto>> GetAllInspectionsAsync();
        Task<List<PendingProductionOrderDto>> GetPendingOrdersForInspectionAsync();
    }
}