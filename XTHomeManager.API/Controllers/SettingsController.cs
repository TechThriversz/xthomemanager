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
            var userId = User.FindFirst("id")?.Value;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var settings = await _context.Settings.FirstOrDefaultAsync(s => s.UserId == userId);
            if (settings == null)
            {
                settings = new Settings
                {
                    UserId = userId,
                    Currency = "PKR",
                    Country = "Pakistan",
                    DecimalPlaces = 0,
                    DateFormat = "dd/MM/yyyy",
                    WeightUnit = "kg",
                    MilkRatePerLiter = 0
                };
                _context.Settings.Add(settings);
                await _context.SaveChangesAsync();
            }
            return Ok(settings);
        }

        public class SettingsUpdateDto
        {
            public string? Currency { get; set; }
            public string? Country { get; set; }
            public int? DecimalPlaces { get; set; }
            public string? DateFormat { get; set; }
            public string? WeightUnit { get; set; }
            public decimal? MilkRatePerLiter { get; set; }
        }

        [HttpPost]
        public async Task<ActionResult<Settings>> UpdateSettings([FromBody] SettingsUpdateDto dto)
        {
            var userId = User.FindFirst("id")?.Value;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var settings = await _context.Settings.FirstOrDefaultAsync(s => s.UserId == userId);
            if (settings == null) return NotFound();

            // Update only provided fields
            if (dto.Currency != null) settings.Currency = dto.Currency;
            if (dto.Country != null) settings.Country = dto.Country;
            if (dto.DecimalPlaces.HasValue) settings.DecimalPlaces = dto.DecimalPlaces.Value;
            if (dto.DateFormat != null) settings.DateFormat = dto.DateFormat;
            if (dto.WeightUnit != null) settings.WeightUnit = dto.WeightUnit;
            if (dto.MilkRatePerLiter.HasValue) settings.MilkRatePerLiter = dto.MilkRatePerLiter.Value;

            await _context.SaveChangesAsync();
            return Ok(settings);
        }
    }
}