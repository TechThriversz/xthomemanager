using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using XTHomeManager.API.Data;
using XTHomeManager.API.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

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

        [HttpGet("analytics/{recordId}")]
        [Authorize]
        public async Task<ActionResult<object>> GetMilkAnalytics(string recordId, [FromQuery] string? month)
        {
            try
            {
                Console.WriteLine($"GetMilkAnalytics: Received request - recordId: {recordId}, month: {month}");
                if (!int.TryParse(recordId, out var parsedRecordId))
                {
                    Console.WriteLine($"GetMilkAnalytics: Invalid recordId - {recordId}");
                    return BadRequest("Record ID must be a valid integer");
                }
                if (!await _context.Records.AnyAsync(r => r.Id == parsedRecordId))
                {
                    Console.WriteLine($"GetMilkAnalytics: Record not found - ID: {parsedRecordId}");
                    return BadRequest("Invalid Record ID");
                }

                var query = _context.MilkEntries
                    .Where(m => m.RecordId == parsedRecordId);

                if (!string.IsNullOrEmpty(month))
                {
                    if (!DateTime.TryParse($"{month}-01", out var monthDate))
                    {
                        Console.WriteLine($"GetMilkAnalytics: Invalid month format - {month}");
                        return BadRequest("Invalid month format, use yyyy-MM");
                    }
                    query = query.Where(m => m.Date.Year == monthDate.Year && m.Date.Month == monthDate.Month);
                }

                var totalQuantity = await query.SumAsync(m => m.QuantityLiters);
                var totalCost = await query.SumAsync(m => m.TotalCost);
                var monthlyTotals = await query
                    .GroupBy(m => m.Date.ToString("yyyy-MM"))
                    .Select(g => new { month = g.Key, totalCost = g.Sum(m => m.TotalCost) })
                    .ToListAsync();
                var statusCounts = await query
                    .GroupBy(m => m.Status)
                    .Select(g => new { status = g.Key, count = g.Count() })
                    .ToDictionaryAsync(g => g.status, g => g.count);

                Console.WriteLine($"GetMilkAnalytics: Result - recordId: {parsedRecordId}, totalQuantity: {totalQuantity}, totalCost: {totalCost}, monthlyTotals: {System.Text.Json.JsonSerializer.Serialize(monthlyTotals)}, statusCounts: {System.Text.Json.JsonSerializer.Serialize(statusCounts)}");
                return new { recordId = parsedRecordId, totalQuantity, totalCost, monthlyTotals, statusCounts };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"GetMilkAnalytics: Error - {ex.Message}, StackTrace: {ex.StackTrace}");
                return StatusCode(500, "An error occurred while fetching milk analytics: " + ex.Message);
            }
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<MilkEntry>> CreateMilkEntry([FromBody] MilkEntry entry)
        {
            try
            {
                using var reader = new StreamReader(Request.Body);
                var body = await reader.ReadToEndAsync();
                Console.WriteLine($"CreateMilkEntry: Raw request body - {body}");

                Console.WriteLine($"CreateMilkEntry: Deserialized payload - {System.Text.Json.JsonSerializer.Serialize(entry)}");

                ModelState.Remove("Record");

                if (!ModelState.IsValid)
                {
                    Console.WriteLine($"CreateMilkEntry: Model validation failed - {System.Text.Json.JsonSerializer.Serialize(ModelState)}");
                    return BadRequest(ModelState);
                }

                if (!await _context.Records.AnyAsync(r => r.Id == entry.RecordId))
                {
                    Console.WriteLine($"CreateMilkEntry: Invalid Record ID - {entry.RecordId}");
                    return BadRequest("Invalid Record ID");
                }

                var settings = await _context.Settings.FirstOrDefaultAsync();
                var milkRatePerLiter = settings?.MilkRatePerLiter ?? 0m;
                entry.TotalCost = entry.QuantityLiters * milkRatePerLiter;

                _context.MilkEntries.Add(entry);
                await _context.SaveChangesAsync();
                Console.WriteLine($"CreateMilkEntry: Successfully created entry ID: {entry.Id}");
                return CreatedAtAction(nameof(GetMilkAnalytics), new { recordId = entry.RecordId }, entry);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"CreateMilkEntry: Error - {ex.Message}, StackTrace: {ex.StackTrace}");
                return StatusCode(500, "An error occurred while creating the milk entry: " + ex.Message);
            }
        }

        [HttpGet("{recordId}")]
        [Authorize]
        public async Task<ActionResult<IEnumerable<MilkEntry>>> GetMilkEntries(int recordId)
        {
            try
            {
                Console.WriteLine($"GetMilkEntries: Fetching milk entries for recordId: {recordId}");
                if (!await _context.Records.AnyAsync(r => r.Id == recordId))
                {
                    Console.WriteLine($"GetMilkEntries: Invalid Record ID - {recordId}");
                    return BadRequest("Invalid Record ID");
                }
                var settings = await _context.Settings.FirstOrDefaultAsync();
                var milkRatePerLiter = settings?.MilkRatePerLiter ?? 0m;
                Console.WriteLine($"GetMilkEntries: Milk rate per liter - {milkRatePerLiter}");
                var entries = await _context.MilkEntries
                    .Where(m => m.RecordId == recordId)
                    .ToListAsync();
                return Ok(entries ?? new List<MilkEntry>());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"GetMilkEntries: Error - {ex.Message}, StackTrace: {ex.StackTrace}");
                return StatusCode(500, "An error occurred while fetching milk entries: " + ex.Message);
            }
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteMilkEntry(int id)
        {
            try
            {
                Console.WriteLine($"DeleteMilkEntry: Attempting to delete milk entry ID: {id}");
                var entry = await _context.MilkEntries.FindAsync(id);
                if (entry == null)
                {
                    Console.WriteLine($"DeleteMilkEntry: Milk entry not found - ID: {id}");
                    return NotFound("Milk entry not found");
                }
                _context.MilkEntries.Remove(entry);
                await _context.SaveChangesAsync();
                Console.WriteLine($"DeleteMilkEntry: Successfully deleted milk entry ID: {id}");
                return NoContent();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"DeleteMilkEntry: Error - {ex.Message}, StackTrace: {ex.StackTrace}");
                return StatusCode(500, "An error occurred while deleting the milk entry: " + ex.Message);
            }
        }
    }
}