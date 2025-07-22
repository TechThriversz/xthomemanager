using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using XTHomeManager.API.Data;
using XTHomeManager.API.Models;

namespace XTHomeManager.Controllers
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
        [Authorize]
        public async Task<ActionResult<IEnumerable<MilkEntry>>> GetMilk(string recordId)
        {
            if (!int.TryParse(recordId, out var parsedRecordId))
                return BadRequest("Record ID must be a valid integer");
            if (!await _context.Records.AnyAsync(r => r.Id == parsedRecordId))
                return BadRequest("Invalid Record ID");
            return await _context.MilkEntries.Where(m => m.RecordId == parsedRecordId).ToListAsync();
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<MilkEntry>> CreateMilk(MilkEntry milk)
        {
            if (!await _context.Records.AnyAsync(r => r.Id == milk.RecordId))
                return BadRequest("Invalid Record ID");
            milk.AdminId = User.FindFirst("id")?.Value ?? throw new UnauthorizedAccessException("Admin ID not found");
            _context.MilkEntries.Add(milk); // Id is auto-incremented
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetMilk), new { recordId = milk.RecordId }, milk);
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteMilk(string id)
        {
            if (!int.TryParse(id, out var parsedId))
                return BadRequest("Milk ID must be a valid integer");
            var milk = await _context.MilkEntries.FindAsync(parsedId);
            if (milk == null) return NotFound("Milk entry not found");
            _context.MilkEntries.Remove(milk);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpGet("analytics/{recordId}")]
        [Authorize]
        public async Task<ActionResult<object>> GetMilkAnalytics(string recordId, [FromQuery] string? month)
        {
            if (!int.TryParse(recordId, out var parsedRecordId))
                return BadRequest("Record ID must be a valid integer");
            if (!await _context.Records.AnyAsync(r => r.Id == parsedRecordId))
                return BadRequest("Invalid Record ID");
            var query = _context.MilkEntries.Where(m => m.RecordId == parsedRecordId && m.Status != "Leave");
            if (!string.IsNullOrEmpty(month))
            {
                if (!DateTime.TryParse($"{month}-01", out var monthDate))
                    return BadRequest("Invalid month format, use yyyy-MM");
                query = query.Where(m => m.Date.Year == monthDate.Year && m.Date.Month == monthDate.Month);
            }
            var totalQuantity = await query.SumAsync(m => m.QuantityLiters);
            var totalCost = await query.SumAsync(m => m.TotalCost);
            return new { recordId = parsedRecordId, totalQuantity, totalCost };
        }
    }
}