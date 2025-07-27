using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using XTHomeManager.API.Data;
using XTHomeManager.API.Models;

namespace XTHomeManager.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SettingsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public SettingsController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<Settings>> GetSettings()
        {
            var userId = User.FindFirst("AdminId")?.Value ?? User.Identity.Name;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("User ID not found in token.");
            }

            var settings = await _context.Settings.FirstOrDefaultAsync(s => s.UserId == userId);
            if (settings == null)
            {
                settings = new Settings { UserId = userId, MilkRatePerLiter = 0 };
                _context.Settings.Add(settings);
                await _context.SaveChangesAsync();
            }
            return Ok(settings);
        }

        public class SettingsUpdateDto
        {
            public decimal MilkRatePerLiter { get; set; }
        }

        [HttpPost]
        public async Task<ActionResult<Settings>> UpdateSettings([FromBody] SettingsUpdateDto settingsDto)
        {
            var userId = User.FindFirst("AdminId")?.Value ?? User.Identity.Name;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("User ID not found in token.");
            }

            var existing = await _context.Settings.FirstOrDefaultAsync(s => s.UserId == userId);
            if (existing == null)
            {
                return NotFound("Settings not found for the user.");
            }

            existing.MilkRatePerLiter = settingsDto.MilkRatePerLiter;
            await _context.SaveChangesAsync();
            return Ok(existing);
        }
    }
}