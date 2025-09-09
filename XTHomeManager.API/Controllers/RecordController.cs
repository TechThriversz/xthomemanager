using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;
using XTHomeManager.API.Data;
using XTHomeManager.API.Models;
using XTHomeManager.API.Services;

namespace XTHomeManager.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RecordController : ControllerBase
    {
        private readonly AppDbContext _context;

        public RecordController(AppDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        [HttpGet]
        [Authorize]
        public async Task<ActionResult<IEnumerable<Record>>> GetRecords()
        {
            var userId = User.FindFirst("id")?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                Console.WriteLine("GetRecords: User ID not found in JWT");
                return Unauthorized(new { Message = "User ID not found" });
            }

            var records = await _context.Records
                .Where(r => r.UserId == userId) // Only return records owned by the user
                .ToListAsync();

            return Ok(records);
        }
        [HttpGet("viewer-records/{userId}")]
        public async Task<ActionResult<List<RecordDto>>> GetViewerRecords(string userId)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null) return NotFound();

            // Fetch only records where the user is an invited viewer, excluding their own records
            var records = await _context.Records
                .Where(r => r.UserId != userId && _context.RecordViewers.Any(rv => rv.RecordId == r.Id && rv.UserId == userId && rv.AllowViewerAccess && rv.IsAccepted))
                .Select(r => new RecordDto
                {
                    Id = r.Id,
                    Name = r.Name,
                    Type = r.Type,
                    IsAccepted = true // Since we filter for IsAccepted, this is redundant but kept for consistency
                })
                .ToListAsync();

            return Ok(records);
        }

        [HttpPost]
        [Authorize]
        public async Task<ActionResult<Record>> CreateRecord([FromBody] CreateRecordDto recordDto)
        {
            try
            {
                Console.WriteLine($"CreateRecord: Received payload - {JsonSerializer.Serialize(recordDto)}");
                if (!ModelState.IsValid)
                {
                    Console.WriteLine($"CreateRecord: Model validation failed - {JsonSerializer.Serialize(ModelState)}");
                    return BadRequest(new { Message = "Validation failed", Errors = ModelState });
                }

                if (!new[] { "Milk", "Bill", "Rent" }.Contains(recordDto.Type))
                {
                    Console.WriteLine($"CreateRecord: Invalid type - Type: {recordDto.Type}");
                    return BadRequest(new { Message = "Type must be Milk, Bill, or Rent" });
                }

                var authUserId = User.FindFirst("id")?.Value;
                if (string.IsNullOrEmpty(authUserId))
                {
                    Console.WriteLine("CreateRecord: No user ID in JWT");
                    return Unauthorized(new { Message = "Authenticated user ID not found" });
                }

                // Map the DTO to the entity and set the user ID from the JWT
                var record = new Record
                {
                    Name = recordDto.Name,
                    Type = recordDto.Type,
                    UserId = authUserId
                };

                _context.Records.Add(record);
                await _context.SaveChangesAsync();
                Console.WriteLine($"CreateRecord: Successfully created record ID: {record.Id}");
                return CreatedAtAction(nameof(GetRecords), new { id = record.Id }, new { Message = "Record created successfully", Record = record });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"CreateRecord: Error - {ex.Message}, StackTrace: {ex.StackTrace}");
                return StatusCode(500, new { Message = "An error occurred while creating the record: " + ex.Message });
            }
        }
        // RecordController.cs (Updated GetRecordDetails)
        [HttpGet("details/{recordId}")]
        [Authorize]
        public async Task<ActionResult<RecordDetailsDto>> GetRecordDetails(int recordId)
        {
            var userId = User.FindFirst("id")?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { Message = "User ID not found" });

            var record = await _context.Records
                .Include(r => r.Viewers)
                .FirstOrDefaultAsync(r => r.Id == recordId);

            if (record == null)
                return NotFound(new { Message = "Record not found" });

            // Check if user is the owner or an invited viewer
            var isOwner = record.UserId == userId;
            var isViewer = record.Viewers != null && record.Viewers.Any(rv => rv.UserId == userId && rv.AllowViewerAccess && rv.IsAccepted);

            if (!isOwner && !isViewer)
                return Unauthorized(new { Message = "Access denied" });

            // Fetch the creator's full name
            var creator = await _context.Users.FirstOrDefaultAsync(u => u.Id == record.UserId);
            var creatorName = creator?.FullName ?? "Unknown";

            var details = new RecordDetailsDto
            {
                Id = record.Id,
                Name = record.Name,
                Type = record.Type,
                CreatedBy = creatorName, // Updated to use FullName instead of UserId
                Viewers = record.Viewers != null
                    ? record.Viewers
                        .Where(rv => rv.AllowViewerAccess && rv.IsAccepted)
                        .Select(rv => new ViewerDto { UserId = rv.UserId, Email = _context.Users.FirstOrDefault(u => u.Id == rv.UserId)?.Email })
                        .ToList()
                    : new List<ViewerDto>()
            };

            // Add entries based on record type with explicit casting
            switch (record.Type.ToLower())
            {
                case "milk":
                    details.Entries = (await _context.MilkEntries
                        .Where(m => m.RecordId == recordId)
                        .ToListAsync()).Cast<object>().ToList();
                    break;
                case "bill":
                    details.Entries = (await _context.ElectricityBills
                        .Where(b => b.RecordId == recordId)
                        .ToListAsync()).Cast<object>().ToList();
                    break;
                case "rent":
                    details.Entries = (await _context.RentEntries
                        .Where(r => r.RecordId == recordId)
                        .ToListAsync()).Cast<object>().ToList();
                    break;
                default:
                    details.Entries = new List<object>(); // Empty list for unsupported types
                    break;
            }

            return Ok(details);
        }

        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> DeleteRecord(string id)
        {
            try
            {
                if (!int.TryParse(id, out var parsedId))
                {
                    Console.WriteLine($"DeleteRecord: Invalid ID - {id}");
                    return BadRequest(new { Message = "Record ID must be a valid integer" });
                }
                var record = await _context.Records.FindAsync(parsedId);
                if (record == null)
                {
                    Console.WriteLine($"DeleteRecord: Record not found - ID: {parsedId}");
                    return NotFound(new { Message = "Record not found" });
                }
                var userId = User.FindFirst("id")?.Value;
                if (record.UserId != userId)
                {
                    Console.WriteLine($"DeleteRecord: Unauthorized - User: {userId}, Record User: {record.UserId}");
                    return BadRequest(new { Message = "Only the record owner can delete it" });
                }
                _context.Records.Remove(record);
                await _context.SaveChangesAsync();
                return Ok(new { Message = "Record deleted successfully" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"DeleteRecord: Error - {ex.Message}, StackTrace: {ex.StackTrace}");
                return StatusCode(500, new { Message = "An error occurred while deleting the record: " + ex.Message });
            }
        }
    }
}
