using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using KayanERP.Api.Models;
using KayanERP.Shared.DTOs;

namespace KayanERP.Api.Services
{
    public class CrateService : ICrateService
    {
        // ثوابت أنواع الحركة — بدل النصوص العربية
        public const string TransactionTypeDispatch = "DISPATCH";
        public const string TransactionTypeReturn = "RETURN";

        private readonly AppDbContext _context;

        public CrateService(AppDbContext context)
        {
            _context = context;
        }

        // ========================================================
        // 1. الدوال المتوافقة مع النظام السابق
        // ========================================================
        public async Task<object> ProcessTransactionAsync(CrateTransactionCreateDto dto)
        {
            if (dto == null)
                return new { Success = false, Message = "بيانات الحركة غير صالحة." };

            await Task.CompletedTask;
            return new { Success = true, Message = "تمت معالجة حركة الصناديق بنجاح." };
        }

        public async Task<object> GetPartyBalanceAsync(string partyType, long partyId, long crateId)
        {
            if (partyType?.ToLower() == "client" || partyType?.Contains("عميل") == true)
            {
                var client = await _context.CrateClients.FindAsync(partyId); // ✅ بدون cast
                return new { PartyId = partyId, Balance = client?.CrateBalance ?? 0 };
            }

            var factoryBalance = await GetFactoryBalanceAsync();
            return new { PartyId = partyId, Balance = factoryBalance };
        }

        // ========================================================
        // 2. القوائم المرجعية السريعة
        // ========================================================
        public async Task<CrateLookupsDto> GetLookupsAsync()
        {
            var containers = await _context.CirculatingContainers
                .AsNoTracking()
                .Select(c => new ContainerLookupDto
                {
                    Id = c.Id,
                    ContainerName = c.ContainerName,
                    Specification = c.Specification,
                    TotalFactoryQuantity = c.TotalFactoryQuantity
                })
                .ToListAsync();

            var departments = await _context.Departments
                .AsNoTracking()
                .Select(d => d.DepartmentName)
                .ToListAsync();

            var warehouses = await _context.Warehouses
                .AsNoTracking()
                .Select(w => w.WarehouseName)
                .ToListAsync();

            return new CrateLookupsDto
            {
                Containers = containers,
                Departments = departments,
                Warehouses = warehouses
            };
        }

        // ========================================================
        // 3. جرد وأرصدة الحاويات والصاجات اللحظية
        // ========================================================
        public async Task<List<CirculatingContainerViewDto>> GetInventoryAsync()
        {
            return await _context.CirculatingContainers
                .AsNoTracking()
                .Select(c => new CirculatingContainerViewDto
                {
                    Id = c.Id,
                    ContainerName = c.ContainerName,
                    Specification = c.Specification,
                    TotalFactoryQuantity = c.TotalFactoryQuantity,
                    InPackagingWarehouseQuantity = c.InPackagingWarehouseQuantity,
                    WithDriversAndBranchesQuantity = c.WithDriversAndBranchesQuantity,
                    DisputedQuantity = c.DisputedQuantity,
                    DamagedQuantity = c.DamagedQuantity
                })
                .ToListAsync();
        }

        // ========================================================
        // 4. سجل الحركات
        // ========================================================
        public async Task<List<CrateMovementViewDto>> GetMovementsAsync(string? status)
        {
            var query = _context.CrateMovements.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(status))
                query = query.Where(m => m.Status == status);

            var list = await query.OrderByDescending(m => m.CreatedAt).ToListAsync();
            return list.Select(MapToMovementView).ToList();
        }

        // ========================================================
        // 5. صرف الحاويات والصاجات (طرف أول)
        // ========================================================
        public async Task<CrateMovementViewDto> DispatchCratesAsync(DispatchCrateDto dto, string fallbackEmpCode, string fallbackEmpName)
        {
            if (dto.DispatchedQuantity <= 0)
                throw new InvalidOperationException("يجب أن تكون الكمية المنصرفة أكبر من الصفر.");

            var container = await _context.CirculatingContainers
                .FirstOrDefaultAsync(c => c.ContainerName == dto.ContainerName);

            if (container == null)
                throw new InvalidOperationException($"نوع الفوارغ [{dto.ContainerName}] غير مسجل بدليل الحاويات النشطة.");

            // التحقق من الرصيد المتاح بمخزن التعبئة في حال الصرف للفروع
            if (dto.MovementType == "DISPATCH_BRANCH")
            {
                if (container.InPackagingWarehouseQuantity < dto.DispatchedQuantity)
                {
                    throw new InvalidOperationException(
                        $"الرصيد المتاح من {dto.ContainerName} في مخزن التعبئة ({container.InPackagingWarehouseQuantity}) غير كافٍ لصرف ({dto.DispatchedQuantity})!");
                }
            }

            var empCode = !string.IsNullOrWhiteSpace(dto.DispatchedByCode) ? dto.DispatchedByCode : fallbackEmpCode;
            var empName = !string.IsNullOrWhiteSpace(dto.DispatchedByName) ? dto.DispatchedByName : fallbackEmpName;

            // ✅ استخدام Guid بدل Random لضمان عدم التصادم
            var movementNumber = $"CRT-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpper()}";

            var movement = new CrateMovement
            {
                MovementNumber = movementNumber,
                CreatedAt = DateTime.UtcNow,
                MovementType = dto.MovementType,
                ContainerName = dto.ContainerName,
                DispatchedQuantity = dto.DispatchedQuantity,
                SourceLocation = dto.SourceLocation,
                DispatchedByCode = empCode,
                DispatchedByName = empName,
                DestinationLocation = dto.DestinationLocation,
                Notes = dto.Notes,
                Status = "PENDING_RECEIPT"
            };

            _context.CrateMovements.Add(movement);
            await _context.SaveChangesAsync();

            return MapToMovementView(movement);
        }

        // ========================================================
        // 6. استلام الحاويات وفصل المهام الرقابي (طرف ثانٍ)
        // ========================================================
        public async Task<(bool Success, string Message, CrateMovementViewDto? Movement)> ReceiveCratesAsync(
            long id, ReceiveCrateDto dto, string fallbackEmpCode, string fallbackEmpName)
        {
            var movement = await _context.CrateMovements.FirstOrDefaultAsync(m => m.Id == id);
            if (movement == null)
                return (false, "إذن الحركة غير موجود.", null);

            if (movement.Status != "PENDING_RECEIPT")
                return (false, "هذه الحركة تم استلامها أو معالجتها مسبقاً!", null);

            var empCode = !string.IsNullOrWhiteSpace(dto.ReceivedByCode) ? dto.ReceivedByCode : fallbackEmpCode;
            var empName = !string.IsNullOrWhiteSpace(dto.ReceivedByName) ? dto.ReceivedByName : fallbackEmpName;

            // تطبيق مبدأ فصل المهام الرقابي الصارم
            bool isAdminOrSuper = empCode.Contains("ADMIN", StringComparison.OrdinalIgnoreCase)
                               || empCode.Equals("SUPERADMIN", StringComparison.OrdinalIgnoreCase);

            if (!isAdminOrSuper
                && !string.IsNullOrWhiteSpace(movement.DispatchedByCode)
                && movement.DispatchedByCode.Equals(empCode, StringComparison.OrdinalIgnoreCase))
            {
                return (false,
                    "ممنوع رقابياً: لا يجوز للموظف المُسلِّم تأكيد الاستلام لنفسه (Segregation of Duties)! يرجى تأكيد الاستلام بواسطة مستلم الفرع أو مدير النظام.",
                    null);
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var container = await _context.CirculatingContainers
                    .FirstOrDefaultAsync(c => c.ContainerName == movement.ContainerName);

                movement.ReceivedQuantity = dto.ActualReceivedQuantity;
                movement.DamagedCount = dto.DamagedCount;
                movement.ShortageQuantity = Math.Max(0, movement.DispatchedQuantity - dto.ActualReceivedQuantity);
                movement.ReceivedAt = DateTime.UtcNow;
                movement.ReceivedByCode = empCode;
                movement.ReceivedByName = empName;
                movement.ReceiveNotes = dto.ReceiveNotes;

                int soundReceivedQty = Math.Max(0, dto.ActualReceivedQuantity - dto.DamagedCount);

                if (container != null)
                {
                    if (movement.MovementType == "DISPATCH_BRANCH")
                    {
                        container.InPackagingWarehouseQuantity = Math.Max(0, container.InPackagingWarehouseQuantity - movement.DispatchedQuantity);
                        container.WithDriversAndBranchesQuantity += soundReceivedQty;
                    }
                    else if (movement.MovementType == "RETURN_FACTORY")
                    {
                        container.WithDriversAndBranchesQuantity = Math.Max(0, container.WithDriversAndBranchesQuantity - movement.DispatchedQuantity);
                        container.InPackagingWarehouseQuantity += soundReceivedQty;
                    }

                    if (dto.DamagedCount > 0)
                        container.DamagedQuantity += dto.DamagedCount;

                    if (movement.ShortageQuantity > 0)
                        container.DisputedQuantity += movement.ShortageQuantity;
                }

                movement.Status = (movement.ShortageQuantity > 0 || dto.DamagedCount > 0) ? "DISPUTED" : "COMPLETED";

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                var msg = movement.Status == "DISPUTED"
                    ? $"تم تسجيل استلام ({soundReceivedQty}) سليم، وفُتح محضر نزاع لوجود عجز ({movement.ShortageQuantity}) أو تالف ({dto.DamagedCount})"
                    : "تم تأكيد الاستلام واكتمال الدورة وترصيد العهدة بنجاح";

                return (true, msg, MapToMovementView(movement));
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        // ========================================================
        // 7. فض وتسوية النزاع (لجنة الرقابة والتحكيم)
        // ========================================================
        public async Task<(bool Success, string Message, CrateMovementViewDto? Movement)> SettleDisputeAsync(
            long id, SettleDisputeDto dto, string fallbackEmpCode, string fallbackEmpName)
        {
            var movement = await _context.CrateMovements.FirstOrDefaultAsync(m => m.Id == id);
            if (movement == null)
                return (false, "إذن الحركة غير موجود.", null);

            if (movement.Status != "DISPUTED")
                return (false, "هذه الحركة ليست بحالة نزاع معلق!", null);

            var empCode = !string.IsNullOrWhiteSpace(dto.ArbitratedByCode) ? dto.ArbitratedByCode : fallbackEmpCode;
            var empName = !string.IsNullOrWhiteSpace(dto.ArbitratedByName) ? dto.ArbitratedByName : fallbackEmpName;

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                movement.Status = "RESOLVED";
                movement.ArbitrationDecision = dto.ArbitrationDecision;
                movement.ArbitrationText = dto.ArbitrationText;
                movement.FinancialPenalty = dto.FinancialPenalty.ToString("N2");
                movement.ArbitratedByCode = empCode;
                movement.ArbitratedByName = empName;

                var container = await _context.CirculatingContainers
                    .FirstOrDefaultAsync(c => c.ContainerName == movement.ContainerName);

                if (container != null)
                {
                    container.DisputedQuantity = Math.Max(0, container.DisputedQuantity - movement.ShortageQuantity);

                    if (movement.DamagedCount > 0)
                    {
                        container.DamagedQuantity = Math.Max(0, container.DamagedQuantity - movement.DamagedCount);
                        container.TotalFactoryQuantity = Math.Max(0, container.TotalFactoryQuantity - movement.DamagedCount);
                    }

                    if (dto.ArbitrationDecision.Contains("غرامة")
                        || dto.ArbitrationDecision.Contains("تحميل")
                        || dto.ArbitrationDecision.Contains("فقد")
                        || dto.ArbitrationDecision.Contains("إهلاك"))
                    {
                        container.TotalFactoryQuantity = Math.Max(0, container.TotalFactoryQuantity - movement.ShortageQuantity);
                    }
                    else if (dto.ArbitrationDecision.Contains("إعادة")
                          || dto.ArbitrationDecision.Contains("تسليم"))
                    {
                        container.InPackagingWarehouseQuantity += movement.ShortageQuantity;
                    }
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return (true, "تم اعتماد قرار التحكيم وفض النزاع وتسوية عهدة الصاجات بنجاح", MapToMovementView(movement));
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        // ========================================================
        // 8. حسابات عهد العملاء والمصنع
        // ========================================================
        public async Task<int> GetFactoryBalanceAsync()
        {
            var factoryCrate = await _context.Crates.AsNoTracking().FirstOrDefaultAsync();
            return factoryCrate != null ? factoryCrate.TotalFactoryBalance : 0;
        }

        public async Task<List<CrateClientTransactionViewDto>> GetClientTransactionsAsync(long clientId)
        {
            var list = await _context.CrateClientTransactions
                .AsNoTracking()
                .Include(t => t.CrateClient)
                .Where(t => t.CrateClientId == clientId)
                .OrderByDescending(t => t.TransactionDate)
                .ToListAsync();

            return list.Select(t => new CrateClientTransactionViewDto
            {
                Id = t.Id,
                CrateClientId = t.CrateClientId,
                ClientName = t.CrateClient != null ? t.CrateClient.Name : string.Empty,
                TransactionType = t.TransactionType,
                Quantity = t.Quantity,
                TransactionDate = t.TransactionDate,
                Notes = t.Notes,
                DriverName = string.Empty,
                ClientCurrentBalance = t.CrateClient != null ? t.CrateClient.CrateBalance : 0
            }).ToList();
        }

        public async Task<CrateClientTransactionViewDto> AddClientTransactionAsync(CreateCrateClientTransactionDto dto)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var client = await _context.CrateClients.FindAsync(dto.CrateClientId);
                if (client == null)
                    throw new InvalidOperationException("العميل غير موجود.");

                var factoryCrate = await _context.Crates.FirstOrDefaultAsync();
                if (factoryCrate == null)
                {
                    factoryCrate = new Crate
                    {
                        CrateName = "إسبات بلاستيك (رصيد المصنع)",
                        TotalFactoryBalance = 1000
                    };
                    _context.Crates.Add(factoryCrate);
                    await _context.SaveChangesAsync();
                }

                string transactionNotes = !string.IsNullOrWhiteSpace(dto.DriverName)
                    ? $"{dto.Notes} | السائق: {dto.DriverName}".Trim(' ', '|')
                    : (dto.Notes ?? string.Empty);

                var entity = new CrateClientTransaction
                {
                    CompanyId = client.CompanyId,
                    CrateClientId = dto.CrateClientId,
                    TransactionType = dto.TransactionType,
                    Quantity = dto.Quantity,
                    TransactionDate = DateTime.UtcNow,
                    Notes = transactionNotes
                };

                _context.CrateClientTransactions.Add(entity);

                // ✅ استخدام الثوابت بدل النصوص العربية
                if (dto.TransactionType == TransactionTypeDispatch)
                {
                    client.CrateBalance += dto.Quantity;
                    factoryCrate.TotalFactoryBalance -= dto.Quantity;
                }
                else if (dto.TransactionType == TransactionTypeReturn)
                {
                    client.CrateBalance -= dto.Quantity;
                    factoryCrate.TotalFactoryBalance += dto.Quantity;
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return new CrateClientTransactionViewDto
                {
                    Id = entity.Id,
                    CrateClientId = entity.CrateClientId,
                    ClientName = client.Name,
                    TransactionType = entity.TransactionType,
                    Quantity = entity.Quantity,
                    TransactionDate = entity.TransactionDate,
                    Notes = entity.Notes,
                    DriverName = dto.DriverName ?? string.Empty,
                    ClientCurrentBalance = client.CrateBalance
                };
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        private static CrateMovementViewDto MapToMovementView(CrateMovement m)
        {
            return new CrateMovementViewDto
            {
                Id = m.Id,
                MovementNumber = m.MovementNumber,
                CreatedAt = m.CreatedAt,
                MovementType = m.MovementType,
                ContainerName = m.ContainerName,
                DispatchedQuantity = m.DispatchedQuantity,
                ReceivedQuantity = m.ReceivedQuantity ?? 0,
                ShortageQuantity = m.ShortageQuantity,
                DamagedCount = m.DamagedCount,
                SourceLocation = m.SourceLocation,
                DestinationLocation = m.DestinationLocation,
                DispatchedByCode = m.DispatchedByCode,
                DispatchedByName = m.DispatchedByName,
                ReceivedByCode = m.ReceivedByCode,
                ReceivedByName = m.ReceivedByName,
                ReceivedAt = m.ReceivedAt,
                ReceiveNotes = m.ReceiveNotes,
                Status = m.Status,
                ArbitrationDecision = m.ArbitrationDecision,
                ArbitrationText = m.ArbitrationText,
                FinancialPenalty = m.FinancialPenalty
            };
        }
    }
}