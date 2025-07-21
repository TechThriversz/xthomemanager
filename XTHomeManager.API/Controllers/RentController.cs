using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using XTHomeManager.API.Data;
using XTHomeManager.API.Models;

namespace XTHomeManager.API.Controllers
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
        public async Task<ActionResult<IEnumerable<RentEntry>>> GetRent(int recordId)
        {
            var userId = User.FindFirst("AdminId")?.Value ?? User.Identity.Name;
            var role = User.FindFirst(ClaimTypes.Role)?.Value;
            var record = await _context.Records.FindAsync(recordId);
            if (record == null || (!record.AllowViewerAccess && role == "Viewer" && record.ViewerId != userId) || (role == "Admin" && record.UserId != userId))
                return Unauthorized();

            return await _context.RentEntries.Where(r => r.RecordId == recordId).ToListAsync();
        }

        [HttpPost]
        public async Task<ActionResult<RentEntry>> CreateRent(RentEntry rent)
        {
            var record = await _context.Records.FindAsync(rent.RecordId);
            var userId = User.FindFirst("AdminId")?.Value ?? User.Identity.Name;
            if (record == null || record.UserId != userId)
                return Unauthorized();

            rent.AdminId = userId;
            _context.RentEntries.Add(rent);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetRent), new { recordId = rent.RecordId }, rent);
        }

        [HttpGet("analytics/{recordId}")]
        public async Task<ActionResult> GetRentAnalytics(int recordId, string month)
        {
            var userId = User.FindFirst("AdminId")?.Value ?? User.Identity.Name;
            var role = User.FindFirst(ClaimTypes.Role)?.Value;
            var record = await _context.Records.FindAsync(recordId);
            if (record == null || (!record.AllowViewerAccess && role == "Viewer" && record.ViewerId != userId) || (role == "Admin" && record.UserId != userId))
                return Unauthorized();

            var query = _context.RentEntries.Where(r => r.RecordId == recordId);
            if (!string.IsNullOrEmpty(month))
                query = query.Where(r => r.Month == month);

            var analytics = await query
                .GroupBy(r => r.Month)
                .Select(g => new
                {
                    Month = g.Key,
                    TotalAmount = g.Sum(r => r.Amount),
                    RentCount = g.Count()
                })
                .ToListAsync();

            return Ok(analytics);
        }
    }
}