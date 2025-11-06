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
        [Authorize]
        public async Task<ActionResult<IEnumerable<SharedRecordDto>>> GetViewerRecords(string userId)
        {
            var authUserId = User.FindFirst("id")?.Value;
            if (authUserId != userId)
                return Forbid();

            var viewerRecords = await _context.RecordViewers
                .Where(rv => rv.UserId == userId && rv.AllowViewerAccess)
                .Select(rv => new SharedRecordDto
                {
                    Id = rv.Record.Id,
                    Name = rv.Record.Name,
                    Type = rv.Record.Type,
                    OwnerName = rv.Record.User.FullName ?? rv.Record.User.Email,
                    IsAccepted = rv.IsAccepted
                })
                .ToListAsync();

            return Ok(viewerRecords);
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
        [HttpGet("details/{recordId}")]
        [Authorize]
        public async Task<ActionResult<RecordDetailsDto>> GetRecordDetails(int recordId)
        {
            var userId = User.FindFirst("id")?.Value;
            var record = await _context.Records
                .Include(r => r.User)
                .FirstOrDefaultAsync(r => r.Id == recordId);

            if (record == null)
                return NotFound();

            // Check if owner or viewer
            if (record.UserId != userId)
            {
                var isViewer = await _context.RecordViewers
                    .AnyAsync(rv => rv.RecordId == recordId && rv.UserId == userId && rv.AllowViewerAccess);
                if (!isViewer) return Forbid();
            }

            var details = new RecordDetailsInvitedDto
            {
                Id = record.Id,
                Name = record.Name,
                Type = record.Type,
                CreatedBy = record.User.FullName ?? record.User.Email,
                Entries = await GetEntriesForType(record.Type, recordId)
            };

            return Ok(details);
        }

        private async Task<List<EntryDto>> GetEntriesForType(string type, int recordId)
        {
            switch (type)
            {
                case "Milk":
                    return await _context.MilkEntries
                        .Where(e => e.RecordId == recordId)
                        .Select(e => new EntryDto
                        {
                            Date = e.Date,
                            QuantityLiters = e.QuantityLiters,
                            Status = e.Status,
                            TotalCost = e.TotalCost
                        })
                        .ToListAsync();

                case "Rent":
                    return await _context.RentEntries
                        .Where(e => e.RecordId == recordId)
                        .Select(e => new EntryDto
                        {
                            Month = e.Month,
                            Amount = e.Amount,
                            Status = e.Status
                        })
                        .ToListAsync();

                case "Bill":
                    return await _context.ElectricityBills
                        .Where(e => e.RecordId == recordId)
                        .Select(e => new EntryDto
                        {
                            Month = e.Month,
                            Amount = e.Amount,
                            ReferenceNumber = e.ReferenceNumber,
                            FilePath = e.FilePath
                        })
                        .ToListAsync();

                default:
                    return new List<EntryDto>();
            }
        }

        [HttpPost("accept-invite/{recordId}")]
        [Authorize]
        public async Task<ActionResult> AcceptInvite(int recordId)
        {
            var userId = User.FindFirst("id")?.Value;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var rv = await _context.RecordViewers
                .FirstOrDefaultAsync(x => x.RecordId == recordId && x.UserId == userId);

            if (rv == null) return NotFound("Invite not found");
            if (!rv.AllowViewerAccess) return Forbid();

            rv.IsAccepted = true;
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Invite accepted!" });
        }

        [HttpPost("decline-invite/{recordId}")]
        [Authorize]
        public async Task<ActionResult> DeclineInvite(int recordId)
        {
            var userId = User.FindFirst("id")?.Value;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var rv = await _context.RecordViewers
                .FirstOrDefaultAsync(x => x.RecordId == recordId && x.UserId == userId);

            if (rv == null) return NotFound();
            if (!rv.AllowViewerAccess) return Forbid();

            rv.AllowViewerAccess = false;
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Invite declined." });
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
