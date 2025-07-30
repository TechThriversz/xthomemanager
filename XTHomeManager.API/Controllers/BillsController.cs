using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using XTHomeManager.API.Data;
using XTHomeManager.API.Models;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Linq;

namespace XTHomeManager.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BillsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly AmazonS3Client _s3Client;

        public BillsController(AppDbContext context, AmazonS3Client s3Client)
        {
            _context = context;
            _s3Client = s3Client ?? throw new ArgumentNullException(nameof(s3Client), "S3 client is not initialized.");
        }

        [HttpGet("{recordId}")]
        [Authorize]
        public async Task<ActionResult<IEnumerable<ElectricityBill>>> GetBills(int recordId)
        {
            try
            {
                Console.WriteLine($"GetBills: Fetching bills for recordId: {recordId}");
                if (!await _context.Records.AnyAsync(r => r.Id == recordId))
                {
                    Console.WriteLine($"GetBills: Invalid Record ID - {recordId}");
                    return BadRequest("Invalid Record ID");
                }
                var entries = await _context.ElectricityBills
                    .Where(b => b.RecordId == recordId)
                    .ToListAsync();
                Console.WriteLine($"GetBills: Returned entries - {System.Text.Json.JsonSerializer.Serialize(entries)}");
                return Ok(entries ?? new List<ElectricityBill>());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"GetBills: Error - {ex.Message}, StackTrace: {ex.StackTrace}");
                return StatusCode(500, "An error occurred while fetching bills: " + ex.Message);
            }
        }

        [HttpGet("analytics/{recordId}")]
        [Authorize]
        public async Task<ActionResult<object>> GetBillsAnalytics(int recordId)
        {
            try
            {
                Console.WriteLine($"GetBillsAnalytics: Fetching analytics for recordId: {recordId}");
                if (!await _context.Records.AnyAsync(r => r.Id == recordId))
                {
                    Console.WriteLine($"GetBillsAnalytics: Invalid Record ID - {recordId}");
                    return BadRequest("Invalid Record ID");
                }

                var monthlyTotals = await _context.ElectricityBills
                    .Where(b => b.RecordId == recordId)
                    .GroupBy(b => b.Month)
                    .Select(g => new { month = g.Key, totalAmount = g.Sum(b => b.Amount) })
                    .ToListAsync();

                Console.WriteLine($"GetBillsAnalytics: Result - {System.Text.Json.JsonSerializer.Serialize(monthlyTotals)}");
                return new { monthlyTotals };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"GetBillsAnalytics: Error - {ex.Message}, StackTrace: {ex.StackTrace}");
                return StatusCode(500, "An error occurred while fetching bill analytics: " + ex.Message);
            }
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ElectricityBill>> CreateBill([FromForm] ElectricityBill entry, IFormFile? file)
        {
            try
            {
                Console.WriteLine($"CreateBill: Received payload - RecordId: {entry.RecordId}, Month: {entry.Month}, Amount: {entry.Amount}, ReferenceNumber: {entry.ReferenceNumber}, AdminId: {entry.AdminId}, File: {(file != null ? file.FileName : "null")}");
                if (!await _context.Records.AnyAsync(r => r.Id == entry.RecordId))
                {
                    Console.WriteLine($"CreateBill: Invalid Record ID - {entry.RecordId}");
                    return BadRequest("Invalid Record ID");
                }
                if (string.IsNullOrEmpty(entry.Month) || !DateTime.TryParse($"{entry.Month}-01", out _))
                {
                    Console.WriteLine($"CreateBill: Invalid Month - {entry.Month}");
                    return BadRequest("Month must be in YYYY-MM format");
                }
                if (string.IsNullOrEmpty(entry.ReferenceNumber))
                {
                    Console.WriteLine($"CreateBill: ReferenceNumber is empty");
                    return BadRequest("Reference Number is required");
                }
                if (entry.Amount <= 0)
                {
                    Console.WriteLine($"CreateBill: Invalid Amount - {entry.Amount}");
                    return BadRequest("Amount must be greater than 0");
                }

                if (file != null && file.Length > 0)
                {
                    var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
                    var uploadRequest = new PutObjectRequest
                    {
                        BucketName = "xthomemanager-uploads",
                        Key = fileName,
                        ContentType = file.ContentType,
                        DisablePayloadSigning = true
                    };

                    using (var memoryStream = new MemoryStream())
                    {
                        await file.CopyToAsync(memoryStream);
                        memoryStream.Position = 0;
                        uploadRequest.InputStream = memoryStream;
                        var response = await _s3Client.PutObjectAsync(uploadRequest);
                        Console.WriteLine($"CreateBill: Upload response - {response.HttpStatusCode}");
                    }

                    entry.FilePath = fileName;
                    Console.WriteLine($"CreateBill: Set FilePath to {entry.FilePath}");
                }
                else
                {
                    entry.FilePath = null;
                    Console.WriteLine($"CreateBill: No file uploaded, FilePath set to null");
                }

                var authUserId = User.FindFirst("id")?.Value;
                if (authUserId != entry.AdminId)
                {
                    Console.WriteLine($"CreateBill: Admin ID mismatch - JWT ID: {authUserId}, Payload ID: {entry.AdminId}");
                    return BadRequest("Admin ID must match authenticated user");
                }

                _context.ElectricityBills.Add(entry);
                await _context.SaveChangesAsync();
                Console.WriteLine($"CreateBill: Successfully created bill ID: {entry.Id}, with FilePath: {entry.FilePath}");
                return CreatedAtAction(nameof(GetBills), new { recordId = entry.RecordId }, entry);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"CreateBill: Error - {ex.Message}, StackTrace: {ex.StackTrace}");
                return StatusCode(500, "An error occurred while creating the bill entry: " + ex.Message);
            }
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteBill(int id)
        {
            try
            {
                Console.WriteLine($"DeleteBill: Attempting to delete bill ID: {id}");
                var bill = await _context.ElectricityBills.FindAsync(id);
                if (bill == null)
                {
                    Console.WriteLine($"DeleteBill: Bill not found - ID: {id}");
                    return NotFound("Bill entry not found");
                }
                _context.ElectricityBills.Remove(bill);
                await _context.SaveChangesAsync();
                Console.WriteLine($"DeleteBill: Successfully deleted bill ID: {id}");
                return NoContent();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"DeleteBill: Error - {ex.Message}, StackTrace: {ex.StackTrace}");
                return StatusCode(500, "An error occurred while deleting the bill entry");
            }
        }
    }
}