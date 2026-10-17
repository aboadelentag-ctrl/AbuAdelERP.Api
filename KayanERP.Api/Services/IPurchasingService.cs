using System.Collections.Generic;
using System.Threading.Tasks;
using KayanERP.Shared.DTOs;

namespace KayanERP.Api.Services
{
    public interface IPurchasingService
    {
        Task<bool> CreateInvoiceAsync(PurchaseInvoiceCreateDto dto);
        Task<List<PurchaseOrderLookupDto>> GetApprovedPurchaseOrdersAsync();
        Task<bool> ReceiveGoodsAsync(MaterialReceiptDto dto);
    }
}