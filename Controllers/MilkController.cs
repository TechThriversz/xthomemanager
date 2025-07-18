using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using XTHomeManager.API.Data;
using XTHomeManager.API.Models;

namespace XTHomeManager.API.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class MilkController : ControllerBase
    {
        private readonly AppDbContext _context;

        public MilkController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetMilkEntries()
        {
            var userId = User.FindFirst("AdminId")?.Value;
            var isAdmin = User.IsInRole("Admin");
            var entries = await _context.MilkEntries
                .Where(e => e.AdminId == userId && (isAdmin || e.AllowViewerAccess))
                .Select(e => new { e.Id, e.Date, e.QuantityLiters, e.Status, e.TotalCost })
                .ToListAsync();
            return Ok(entries);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> CreateMilkEntry([FromBody] MilkEntry entry)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            if (entry.Status != "Bought" && entry.Status != "Leave") return BadRequest("Invalid status");

            entry.AdminId = User.FindFirst("AdminId")?.Value;
            _context.MilkEntries.Add(entry);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetMilkEntries), new { id = entry.Id }, entry);
        }
    }
}