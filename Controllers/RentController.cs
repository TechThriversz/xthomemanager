using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using XTHomeManager.API.Data;
using XTHomeManager.API.Models;

namespace XTHomeManager.API.Controllers
{
    [Authorize(Roles = "Admin")]
    [Route("api/[controller]")]
    [ApiController]
    public class RentController : ControllerBase
    {
        private readonly AppDbContext _context;

        public RentController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetRentEntries()
        {
            var userId = User.FindFirst("AdminId")?.Value;
            var entries = await _context.RentEntries
                .Where(r => r.AdminId == userId)
                .ToListAsync();
            return Ok(entries);
        }

        [HttpPost]
        public async Task<IActionResult> CreateRentEntry([FromBody] RentEntry entry)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            entry.AdminId = User.FindFirst("AdminId")?.Value;
            _context.RentEntries.Add(entry);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetRentEntries), new { id = entry.Id }, entry);
        }
    }
}