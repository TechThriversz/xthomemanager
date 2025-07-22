using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using XTHomeManager.API.Data;
using XTHomeManager.API.Models;
using System;
using System.Text.Json;

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
        [Authorize]
        public async Task<ActionResult<IEnumerable<Record>>> GetRecords()
        {
            var userId = User.FindFirst("id")?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                Console.WriteLine("GetRecords: User ID not found in JWT");
                return Unauthorized("User ID not found");
            }
            return await _context.Records
                .Where(r => r.UserId == userId || r.ViewerId == userId)
                .ToListAsync();
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<Record>> CreateRecord([FromBody] Record record)
        {
            try
            {
                Console.WriteLine($"CreateRecord: Received payload - {JsonSerializer.Serialize(record)}");
                if (!ModelState.IsValid)
                {
                    Console.WriteLine($"CreateRecord: Model validation failed - {JsonSerializer.Serialize(ModelState)}");
                    return BadRequest(ModelState);
                }

                if (string.IsNullOrEmpty(record.Name) || record.Name.Length > 100)
                {
                    Console.WriteLine($"CreateRecord: Invalid name - Name: {record.Name}");
                    return BadRequest("Record name is required and must be 100 characters or less");
                }
                if (!new[] { "Milk", "Bill", "Rent" }.Contains(record.Type))
                {
                    Console.WriteLine($"CreateRecord: Invalid type - Type: {record.Type}");
                    return BadRequest("Type must be Milk, Bill, or Rent");
                }
                if (string.IsNullOrEmpty(record.UserId))
                {
                    Console.WriteLine("CreateRecord: User ID is empty or null");
                    return BadRequest("User ID is required");
                }

                var authUserId = User.FindFirst("id")?.Value;
                if (string.IsNullOrEmpty(authUserId))
                {
                    Console.WriteLine("CreateRecord: No user ID in JWT");
                    return Unauthorized("Authenticated user ID not found");
                }
                if (!string.Equals(authUserId, record.UserId, StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine($"CreateRecord: User ID mismatch - JWT ID: {authUserId}, Payload ID: {record.UserId}");
                    return BadRequest("User ID in request does not match authenticated user");
                }

                record.ViewerId = null; // Default for new records
                _context.Records.Add(record);
                await _context.SaveChangesAsync();
                Console.WriteLine($"CreateRecord: Successfully created record ID: {record.Id}");
                return CreatedAtAction(nameof(GetRecords), new { id = record.Id }, record);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"CreateRecord: Error - {ex.Message}, StackTrace: {ex.StackTrace}");
                return StatusCode(500, "An error occurred while creating the record: " + ex.Message);
            }
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteRecord(string id)
        {
            try
            {
                if (!int.TryParse(id, out var parsedId))
                {
                    Console.WriteLine($"DeleteRecord: Invalid ID - {id}");
                    return BadRequest("Record ID must be a valid integer");
                }
                var record = await _context.Records.FindAsync(parsedId);
                if (record == null)
                {
                    Console.WriteLine($"DeleteRecord: Record not found - ID: {parsedId}");
                    return NotFound("Record not found");
                }
                if (record.UserId != User.FindFirst("id")?.Value)
                {
                    Console.WriteLine($"DeleteRecord: Unauthorized - User: {User.FindFirst("id")?.Value}, Record User: {record.UserId}");
                    return Forbid("Only the record owner can delete it");
                }
                _context.Records.Remove(record);
                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"DeleteRecord: Error - {ex.Message}, StackTrace: {ex.StackTrace}");
                return StatusCode(500, "An error occurred while deleting the record: " + ex.Message);
            }
        }
    }
}