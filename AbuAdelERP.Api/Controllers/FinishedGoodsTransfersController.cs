using Microsoft.AspNetCore.Mvc;
using AbuAdelERP.Api.DTOs;
using AbuAdelERP.Api.Services;
using System;
using System.Threading.Tasks;

namespace AbuAdelERP.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FinishedGoodsTransfersController : ControllerBase
    {
        private readonly IFinishedGoodsTransferService _transferService;

        public FinishedGoodsTransfersController(IFinishedGoodsTransferService transferService)
        {
            _transferService = transferService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var list = await _transferService.GetAllTransfersAsync();
            return Ok(list);
        }

        [HttpPost("create")]
        public async Task<IActionResult> Create([FromBody] CreateTransferRequestDto dto)
        {
            var res = await _transferService.CreateTransferAsync(dto);
            return Ok(res);
        }

        [HttpPost("audit-costing")]
        public async Task<IActionResult> AuditCosting([FromBody] CostingAuditDto dto)
        {
            var res = await _transferService.AuditCostingAsync(dto);
            return Ok(res);
        }

        [HttpPost("resolve-costing-dispute")]
        public async Task<IActionResult> ResolveCostingDispute([FromBody] ResolveCostingDisputeDto dto)
        {
            var res = await _transferService.ResolveCostingDisputeAsync(dto);
            return Ok(res);
        }

        [HttpPost("inspect-qc")]
        public async Task<IActionResult> InspectQc([FromBody] QcInspectionDto dto)
        {
            try
            {
                var res = await _transferService.InspectQcAsync(dto);
                return Ok(res);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("resubmit-rework")]
        public async Task<IActionResult> ReSubmitRework([FromBody] ReSubmitReworkDto dto)
        {
            try
            {
                var res = await _transferService.ReSubmitReworkAsync(dto.TransferId, dto.AdjustedQty, dto.AdjustedCount, dto.Notes);
                return Ok(res);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("receive-control")]
        public async Task<IActionResult> ReceiveControl([FromBody] ControlReceiptDto dto)
        {
            try
            {
                var res = await _transferService.ReceiveAtControlAsync(dto);
                return Ok(res);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("resolve-dispute")]
        public async Task<IActionResult> ResolveDispute([FromBody] ResolveDisputeDto dto)
        {
            try
            {
                var res = await _transferService.ResolveDisputeAsync(dto);
                return Ok(res);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}