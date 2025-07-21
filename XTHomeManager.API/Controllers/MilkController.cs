using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using XTHomeManager.API.Data;
using XTHomeManager.API.Models;

namespace XTHomeManager.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MilkController : ControllerBase
    {
        private readonly AppDbContext _context;

        public MilkController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("{recordId}")]
        public async Task<ActionResult<IEnumerable<MilkEntry>>> GetMilk(int recordId)
        {
            var userId = User.FindFirst("AdminId")?.Value ?? User.Identity.Name;
            var role = User.FindFirst(ClaimTypes.Role)?.Value;
            var record = await _context.Records.FindAsync(recordId);
            if (record == null || (!record.AllowViewerAccess && role == "Viewer" && record.ViewerId != userId) || (role == "Admin" && record.UserId != userId))
                return Unauthorized();

            return await _context.MilkEntries.Where(m => m.RecordId == recordId).ToListAsync();
        }

        [HttpPost]
        public async Task<ActionResult<MilkEntry>> CreateMilk(MilkEntry milk)
        {
            var record = await _context.Records.FindAsync(milk.RecordId);
            var userId = User.FindFirst("AdminId")?.Value ?? User.Identity.Name;
            if (record == null || record.UserId != userId)
                return Unauthorized();

            var settings = await _context.Settings.FirstOrDefaultAsync(s => s.UserId == userId);
            milk.RatePerLiter = settings?.MilkRatePerLiter ?? 200; // Default 200 Rs/liter
            milk.AdminId = userId;
            _context.MilkEntries.Add(milk);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetMilk), new { recordId = milk.RecordId }, milk);
        }

        [HttpGet("analytics/{recordId}")]
        public async Task<ActionResult> GetMilkAnalytics(int recordId, string month)
        {
            var userId = User.FindFirst("AdminId")?.Value ?? User.Identity.Name;
            var role = User.FindFirst(ClaimTypes.Role)?.Value;
            var record = await _context.Records.FindAsync(recordId);
            if (record == null || (!record.AllowViewerAccess && role == "Viewer" && record.ViewerId != userId) || (role == "Admin" && record.UserId != userId))
                return Unauthorized();

            var query = _context.MilkEntries.Where(m => m.RecordId == recordId);
            if (!string.IsNullOrEmpty(month))
                query = query.Where(m => m.Date.ToString("yyyy-MM") == month);

            var analytics = await query
                .GroupBy(m => m.Date.ToString("yyyy-MM"))
                .Select(g => new
                {
                    Month = g.Key,
                    TotalQuantity = g.Sum(m => m.Status == "Bought" ? m.QuantityLiters : 0),
                    TotalCost = g.Sum(m => m.Status == "Bought" ? m.QuantityLiters * m.RatePerLiter : 0),
                    BoughtDays = g.Count(m => m.Status == "Bought"),
                    LeaveDays = g.Count(m => m.Status == "Leave")
                })
                .ToListAsync();

            return Ok(analytics);
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteMilk(string id)
        {
            var milk = await _context.MilkEntries.FindAsync(id);
            if (milk == null) return NotFound();
            _context.MilkEntries.Remove(milk);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}