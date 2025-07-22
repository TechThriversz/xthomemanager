using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using XTHomeManager.API.Data;
using XTHomeManager.API.Models;

namespace XTHomeManager.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RentController : ControllerBase
    {
        private readonly AppDbContext _context;

        public RentController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("{recordId}")]
        [Authorize]
        public async Task<ActionResult<IEnumerable<RentEntry>>> GetRent(string recordId)
        {
            if (!int.TryParse(recordId, out var parsedRecordId))
                return BadRequest("Record ID must be a valid integer");
            if (!await _context.Records.AnyAsync(r => r.Id == parsedRecordId))
                return BadRequest("Invalid Record ID");
            return await _context.RentEntries.Where(r => r.RecordId == parsedRecordId).ToListAsync();
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<RentEntry>> CreateRent(RentEntry rent)
        {
            if (!await _context.Records.AnyAsync(r => r.Id == rent.RecordId))
                return BadRequest("Invalid Record ID");
            rent.AdminId = User.FindFirst("id")?.Value ?? throw new UnauthorizedAccessException("Admin ID not found");
            _context.RentEntries.Add(rent); // Id is auto-incremented
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetRent), new { recordId = rent.RecordId }, rent);
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteRent(string id)
        {
            if (!int.TryParse(id, out var parsedId))
                return BadRequest("Rent ID must be a valid integer");
            var rent = await _context.RentEntries.FindAsync(parsedId);
            if (rent == null) return NotFound("Rent entry not found");
            _context.RentEntries.Remove(rent);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpGet("analytics/{recordId}")]
        [Authorize]
        public async Task<ActionResult<object>> GetRentAnalytics(string recordId, [FromQuery] string? month)
        {
            if (!int.TryParse(recordId, out var parsedRecordId))
                return BadRequest("Record ID must be a valid integer");
            if (!await _context.Records.AnyAsync(r => r.Id == parsedRecordId))
                return BadRequest("Invalid Record ID");
            var query = _context.RentEntries.Where(r => r.RecordId == parsedRecordId);
            if (!string.IsNullOrEmpty(month))
            {
                if (!DateTime.TryParse($"{month}-01", out var monthDate))
                    return BadRequest("Invalid month format, use yyyy-MM");
                query = query.Where(r => r.Month == month);
            }
            var totalAmount = await query.SumAsync(r => r.Amount);
            return new { recordId = parsedRecordId, totalAmount };
        }
    }
}