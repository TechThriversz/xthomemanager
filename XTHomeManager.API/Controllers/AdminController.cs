// Controllers/AdminController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using XTHomeManager.API.Data;
using XTHomeManager.API.Models;
using XTHomeManager.API.Services;

[ApiController]
[Route("api/[controller]")]

public class AdminController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly EmailService _emailService;

    public AdminController(AppDbContext context, EmailService emailService)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
    }

    [HttpGet("users")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> GetUsers()
    {
        var users = await _context.Users
            .Include(u => u.ProUpgradeRequests)
            .Select(u => new
            {
                u.Id,
                u.FullName,
                u.Email,
                u.PhoneNumber,
                u.Role,
                u.IsActive,
                u.IsPro,
                u.ProEndDate,
                u.CanUsePasswordVault,
                u.CanUseFamilyMembers,
                u.CanUseMedicalRecords,
                proUpgradeRequests = u.ProUpgradeRequests
                    .Select(r => new { r.Id, r.Status, r.RequestDate })
                    .ToList()
            })
            .ToListAsync();

        return Ok(users);
    }

    [HttpPatch("users/{id}/toggle")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ToggleUser(string id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null) return NotFound();
        user.IsActive = !user.IsActive;
        await _context.SaveChangesAsync();
        return Ok(new { user.Id, user.IsActive });
    }

    [HttpPatch("users/{id}/role")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> SetRole(string id, [FromBody] string role)
    {
        if (!new[] { "Admin", "User" }.Contains(role)) return BadRequest("Invalid role");
        var user = await _context.Users.FindAsync(id);
        if (user == null) return NotFound();
        user.Role = role;
        await _context.SaveChangesAsync();
        return Ok();
    }

    [HttpPatch("users/{id}/permissions")]
    public async Task<IActionResult> UpdatePermissions(string id, [FromBody] JsonElement dto)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null) return NotFound();

        if (dto.TryGetProperty("canUsePasswordVault", out var pv))
            user.CanUsePasswordVault = pv.GetBoolean();
        if (dto.TryGetProperty("canUseFamilyMembers", out var fm))
            user.CanUseFamilyMembers = fm.GetBoolean();
        if (dto.TryGetProperty("canUseMedicalRecords", out var mr))
            user.CanUseMedicalRecords = mr.GetBoolean();

        await _context.SaveChangesAsync();
        return Ok();
    }

    // Pro Upgrade Requests

    [HttpPost("upgrade/request")]
    public async Task<IActionResult> RequestProUpgrade()
    {
        var userId = User.FindFirst("id")?.Value;
        var user = await _context.Users.FindAsync(userId);
        if (user == null) return NotFound();

        if (string.IsNullOrWhiteSpace(user.FullName) || string.IsNullOrWhiteSpace(user.PhoneNumber))
            return BadRequest("Please update your Full Name and Phone Number in Settings first.");

        var existing = await _context.ProUpgradeRequests
            .AnyAsync(r => r.UserId == userId && r.Status == "Pending");
        if (existing) return BadRequest("Request already pending");

        var request = new ProUpgradeRequest { UserId = userId };
        _context.ProUpgradeRequests.Add(request);
        await _context.SaveChangesAsync();

        await _emailService.SendProUpgradeRequestAsync(
            "techthrivers@gmail.com",
            user.FullName,
            user.PhoneNumber,
            user.Email,
            request.RequestDate
        );

        return Ok();
    }

    [HttpPost("upgrade/approve/{userId}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ApprovePro(string userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null) return NotFound();

        user.IsPro = true;
        user.ProStartDate = DateTime.UtcNow;
        user.ProEndDate = DateTime.UtcNow.AddYears(1);

        var request = await _context.ProUpgradeRequests
            .FirstOrDefaultAsync(r => r.UserId == userId && r.Status == "Pending");
        if (request != null)
        {
            request.Status = "Approved";
            request.ApprovedDate = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
        return Ok();
    }
}