using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using XTHomeManager.API.Data;
using XTHomeManager.API.Models;

namespace XTHomeManager.Controllers
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
        [Authorize]
        public async Task<ActionResult<IEnumerable<ElectricityBill>>> GetBills(string recordId)
        {
            if (!int.TryParse(recordId, out var parsedRecordId))
                return BadRequest("Record ID must be a valid integer");
            if (!await _context.Records.AnyAsync(r => r.Id == parsedRecordId))
                return BadRequest("Invalid Record ID");
            return await _context.ElectricityBills.Where(b => b.RecordId == parsedRecordId).ToListAsync();
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ElectricityBill>> CreateBill([FromForm] ElectricityBill bill, [FromForm] IFormFile? file)
        {
            if (!await _context.Records.AnyAsync(r => r.Id == bill.RecordId))
                return BadRequest("Invalid Record ID");
            if (file != null)
            {
                if (file.Length > 5 * 1024 * 1024 || !new[] { ".jpg", ".jpeg", ".png", ".pdf" }.Contains(Path.GetExtension(file.FileName).ToLower()))
                    return BadRequest("Invalid file: max 5MB, only .jpg, .jpeg, .png, .pdf allowed");
                var uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "Uploads");
                if (!Directory.Exists(uploadsDir)) Directory.CreateDirectory(uploadsDir);
                var filePath = Path.Combine(uploadsDir, $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}");
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }
                bill.FilePath = filePath;
            }
            bill.AdminId = User.FindFirst("id")?.Value ?? throw new UnauthorizedAccessException("Admin ID not found");
            _context.ElectricityBills.Add(bill); // Id is auto-incremented by EF Core
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetBills), new { recordId = bill.RecordId }, bill);
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteBill(string id)
        {
            if (!int.TryParse(id, out var parsedId))
                return BadRequest("Bill ID must be a valid integer");
            var bill = await _context.ElectricityBills.FindAsync(parsedId);
            if (bill == null) return NotFound("Bill not found");
            if (!string.IsNullOrEmpty(bill.FilePath) && System.IO.File.Exists(bill.FilePath))
            {
                System.IO.File.Delete(bill.FilePath);
            }
            _context.ElectricityBills.Remove(bill);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpGet("analytics/{recordId}")]
        [Authorize]
        public async Task<ActionResult<object>> GetBillsAnalytics(string recordId, [FromQuery] string? month)
        {
            if (!int.TryParse(recordId, out var parsedRecordId))
                return BadRequest("Record ID must be a valid integer");
            if (!await _context.Records.AnyAsync(r => r.Id == parsedRecordId))
                return BadRequest("Invalid Record ID");
            var query = _context.ElectricityBills.Where(b => b.RecordId == parsedRecordId);
            if (!string.IsNullOrEmpty(month))
            {
                if (!DateTime.TryParse($"{month}-01", out var monthDate))
                    return BadRequest("Invalid month format, use yyyy-MM");
                query = query.Where(b => b.Month == month);
            }
            var totalAmount = await query.SumAsync(b => b.Amount);
            return new { recordId = parsedRecordId, totalAmount };
        }
    }
}