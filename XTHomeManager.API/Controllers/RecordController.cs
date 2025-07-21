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
    public class RecordController : ControllerBase
    {
        private readonly AppDbContext _context;

        public RecordController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Record>>> GetRecords()
        {
            var userId = User.FindFirst("AdminId")?.Value ?? User.Identity.Name;
            var role = User.FindFirst(ClaimTypes.Role)?.Value;
            if (role == "Viewer")
                return await _context.Records.Where(r => r.ViewerId == userId && r.AllowViewerAccess).Include(r => r.User).ToListAsync();
            return await _context.Records.Where(r => r.UserId == userId).Include(r => r.User).ToListAsync();
        }

        [HttpPost]
        public async Task<ActionResult<Record>> CreateRecord(Record record)
        {
            record.UserId = User.FindFirst("AdminId")?.Value ?? User.Identity.Name;
            _context.Records.Add(record);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetRecords), new { id = record.Id }, record);
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteRecord(string id)
        {
            var record = await _context.Records.FindAsync(id);
            if (record == null) return NotFound();
            _context.Records.Remove(record);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}