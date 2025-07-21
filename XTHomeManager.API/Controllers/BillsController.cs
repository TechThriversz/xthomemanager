using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using XTHomeManager.API.Data;
using XTHomeManager.API.Models;

namespace XTHomeManager.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BillsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public BillsController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("{recordId}")]
        public async Task<ActionResult<IEnumerable<ElectricityBill>>> GetBills(int recordId)
        {
            var userId = User.FindFirst("AdminId")?.Value ?? User.Identity.Name;
            var role = User.FindFirst(ClaimTypes.Role)?.Value;
            var record = await _context.Records.FindAsync(recordId);
            if (record == null || (!record.AllowViewerAccess && role == "Viewer" && record.ViewerId != userId) || (role == "Admin" && record.UserId != userId))
                return Unauthorized();

            return await _context.ElectricityBills.Where(b => b.RecordId == recordId).ToListAsync();
        }

        [HttpPost]
        public async Task<ActionResult<ElectricityBill>> CreateBill(ElectricityBill bill)
        {
            var record = await _context.Records.FindAsync(bill.RecordId);
            var userId = User.FindFirst("AdminId")?.Value ?? User.Identity.Name;
            if (record == null || record.UserId != userId)
                return Unauthorized();

            bill.AdminId = userId;
            _context.ElectricityBills.Add(bill);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetBills), new { recordId = bill.RecordId }, bill);
        }

        [HttpGet("analytics/{recordId}")]
        public async Task<ActionResult> GetBillsAnalytics(int recordId, string month)
        {
            var userId = User.FindFirst("AdminId")?.Value ?? User.Identity.Name;
            var role = User.FindFirst(ClaimTypes.Role)?.Value;
            var record = await _context.Records.FindAsync(recordId);
            if (record == null || (!record.AllowViewerAccess && role == "Viewer" && record.ViewerId != userId) || (role == "Admin" && record.UserId != userId))
                return Unauthorized();

            var query = _context.ElectricityBills.Where(b => b.RecordId == recordId);
            if (!string.IsNullOrEmpty(month))
                query = query.Where(b => b.Month == month);

            var analytics = await query
                .GroupBy(b => b.Month)
                .Select(g => new
                {
                    Month = g.Key,
                    TotalAmount = g.Sum(b => b.Amount),
                    BillCount = g.Count()
                })
                .ToListAsync();

            return Ok(analytics);
        }
    }
}