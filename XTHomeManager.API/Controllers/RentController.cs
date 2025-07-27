using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using XTHomeManager.API.Data;
using XTHomeManager.API.Models;
using System;

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
        [Authorize]
        public async Task<ActionResult<IEnumerable<RentEntry>>> GetRent(int recordId)
        {
            try
            {
                Console.WriteLine($"GetRent: Fetching rent entries for recordId: {recordId}");
                if (!await _context.Records.AnyAsync(r => r.Id == recordId))
                {
                    Console.WriteLine($"GetRent: Invalid Record ID - {recordId}");
                    return BadRequest("Invalid Record ID");
                }
                var entries = await _context.RentEntries
                    .Where(r => r.RecordId == recordId)
                    .ToListAsync();
                return Ok(entries ?? new List<RentEntry>());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"GetRent: Error - {ex.Message}, StackTrace: {ex.StackTrace}");
                return StatusCode(500, "An error occurred while fetching rent entries: " + ex.Message);
            }
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<RentEntry>> CreateRent([FromBody] RentEntry entry)
        {
            try
            {
                Console.WriteLine($"CreateRent: Received payload - RecordId: {entry.RecordId}, Month: {entry.Month}, Amount: {entry.Amount}, AdminId: {entry.AdminId}");
                if (!await _context.Records.AnyAsync(r => r.Id == entry.RecordId))
                {
                    Console.WriteLine($"CreateRent: Invalid Record ID - {entry.RecordId}");
                    return BadRequest("Invalid Record ID");
                }
                if (string.IsNullOrEmpty(entry.Month) || !DateTime.TryParse($"{entry.Month}-01", out _))
                {
                    Console.WriteLine($"CreateRent: Invalid Month - {entry.Month}");
                    return BadRequest("Month must be in YYYY-MM format");
                }
                if (entry.Amount <= 0)
                {
                    Console.WriteLine($"CreateRent: Invalid Amount - {entry.Amount}");
                    return BadRequest("Amount must be greater than 0");
                }

                var authUserId = User.FindFirst("id")?.Value;
                if (authUserId != entry.AdminId)
                {
                    Console.WriteLine($"CreateRent: Admin ID mismatch - JWT ID: {authUserId}, Payload ID: {entry.AdminId}");
                    return BadRequest("Admin ID must match authenticated user");
                }

                _context.RentEntries.Add(entry);
                await _context.SaveChangesAsync();
                Console.WriteLine($"CreateRent: Successfully created rent entry ID: {entry.Id}");
                return CreatedAtAction(nameof(GetRent), new { recordId = entry.RecordId }, entry);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"CreateRent: Error - {ex.Message}, StackTrace: {ex.StackTrace}");
                return StatusCode(500, "An error occurred while creating the rent entry");
            }
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteRent(int id)
        {
            try
            {
                Console.WriteLine($"DeleteRent: Attempting to delete rent entry ID: {id}");
                var rent = await _context.RentEntries.FindAsync(id);
                if (rent == null)
                {
                    Console.WriteLine($"DeleteRent: Rent entry not found - ID: {id}");
                    return NotFound("Rent entry not found");
                }
                _context.RentEntries.Remove(rent);
                await _context.SaveChangesAsync();
                Console.WriteLine($"DeleteRent: Successfully deleted rent entry ID: {id}");
                return NoContent();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"DeleteRent: Error - {ex.Message}, StackTrace: {ex.StackTrace}");
                return StatusCode(500, "An error occurred while deleting the rent entry");
            }
        }
    }
}