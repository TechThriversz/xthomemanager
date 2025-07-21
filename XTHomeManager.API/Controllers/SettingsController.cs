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
            var settings = await _context.Settings.FirstOrDefaultAsync(s => s.UserId == userId);
            if (settings == null)
                return NotFound();
            return settings;
        }

        [HttpPost]
        public async Task<ActionResult<Settings>> UpdateSettings(Settings settings)
        {
            var userId = User.FindFirst("AdminId")?.Value ?? User.Identity.Name;
            var existing = await _context.Settings.FirstOrDefaultAsync(s => s.UserId == userId);
            if (existing == null)
            {
                settings.UserId = userId;
                _context.Settings.Add(settings);
            }
            else
            {
                existing.MilkRatePerLiter = settings.MilkRatePerLiter;
            }
            await _context.SaveChangesAsync();
            return Ok(settings);
        }
    }
}