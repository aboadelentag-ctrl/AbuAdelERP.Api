using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AbuAdelERP.Api.Models;

namespace AbuAdelERP.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CrateClientTransactionsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public CrateClientTransactionsController(AppDbContext context)
        {
            _context = context;
        }

        // جلب رصيد المصنع الفعلي لعرضه في الشاشة
        [HttpGet("factory-balance")]
        public async Task<ActionResult<int>> GetFactoryBalance()
        {
            var factoryCrate = await _context.Crates.FirstOrDefaultAsync();
            return factoryCrate != null ? factoryCrate.TotalFactoryBalance : 0;
        }

        [HttpGet("client/{clientId}")]
        public async Task<ActionResult<IEnumerable<CrateClientTransaction>>> GetClientTransactions(int clientId)
        {
            return await _context.CrateClientTransactions
                .Where(t => t.CrateClientId == clientId)
                .OrderByDescending(t => t.TransactionDate)
                .ToListAsync();
        }

        [HttpPost]
        public async Task<ActionResult<CrateClientTransaction>> PostTransaction(CrateClientTransaction transaction)
        {
            transaction.TransactionDate = DateTime.Now;
            _context.CrateClientTransactions.Add(transaction);

            var client = await _context.CrateClients.FindAsync(transaction.CrateClientId);

            // --- نظام القيد المزدوج (الربط مع مخزن المصنع) ---
            var factoryCrate = await _context.Crates.FirstOrDefaultAsync();

            if (factoryCrate == null)
            {
                factoryCrate = new Crate { CrateName = "إسبات بلاستيك (رصيد المصنع)", TotalFactoryBalance = 1000 };
                _context.Crates.Add(factoryCrate);
                await _context.SaveChangesAsync();
            }

            if (client != null)
            {
                if (transaction.TransactionType == "منصرف للعميل")
                {
                    client.CrateBalance += transaction.Quantity;
                    factoryCrate.TotalFactoryBalance -= transaction.Quantity;
                }
                else if (transaction.TransactionType == "مستلم من العميل")
                {
                    client.CrateBalance -= transaction.Quantity;
                    factoryCrate.TotalFactoryBalance += transaction.Quantity;
                }

                _context.Entry(client).State = EntityState.Modified;
                _context.Entry(factoryCrate).State = EntityState.Modified;
            }

            await _context.SaveChangesAsync();

            return Ok(transaction);
        }
    }
}