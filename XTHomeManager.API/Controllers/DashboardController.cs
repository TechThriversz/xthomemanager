using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using XTHomeManager.API.Data;
using System;
using System.Threading.Tasks;

namespace XTHomeManager.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DashboardController : ControllerBase
    {
        private readonly AppDbContext _context;

        public DashboardController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("summary")]
        [Authorize]
        public async Task<ActionResult<object>> GetDashboardSummary()
        {
            try
            {
                Console.WriteLine("GetDashboardSummary: Fetching dashboard summary");
                // Dummy data for now
                var summary = new
                {
                    activeFamilyMembers = 6,
                    totalPasswords = 47,
                    medicalRecords = 12
                };

                Console.WriteLine($"GetDashboardSummary: Result - {System.Text.Json.JsonSerializer.Serialize(summary)}");
                return Ok(summary);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"GetDashboardSummary: Error - {ex.Message}, StackTrace: {ex.StackTrace}");
                return StatusCode(500, "An error occurred while fetching dashboard summary: " + ex.Message);
            }
        }
    }
}