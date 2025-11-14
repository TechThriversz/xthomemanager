// Controllers/AdminController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query.Internal;
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
                u.ImagePath,
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

    [HttpPost("deletion/request")]
    public async Task<IActionResult> RequestAccountDeletion()
    {
        var userId = User.FindFirst("id")?.Value;
        var user = await _context.Users.FindAsync(userId);
        if (user == null) return NotFound();

        var existing = await _context.UserDeletionRequests
            .AnyAsync(r => r.UserId == userId && r.Status == "Pending");
        if (existing) return BadRequest("Deletion request already pending.");

        var request = new UserDeletionRequest { UserId = userId };
        _context.UserDeletionRequests.Add(request);
        await _context.SaveChangesAsync();

        await _emailService.SendDeletionRequestAsync(
            "techthrivers@gmail.com",
            user.FullName,
            user.Email,
            request.RequestDate
        );

        return Ok();
    }


    [HttpPost("deletion/cancel")]
    public async Task<IActionResult> CancelAccountDeletionRequest()
    {

        var userId = User.FindFirst("id")?.Value;
        if (string.IsNullOrEmpty(userId)) return Unauthorized();


        var user = await _context.Users.FindAsync(userId);
        if (user == null) return NotFound("User not found.");

        var request = await _context.UserDeletionRequests
            .FirstOrDefaultAsync(r =>
                r.UserId == userId &&
                (r.Status == "Pending" || r.Status == "Approved"));

        if (request == null)
        {
            return BadRequest("No active or pending deletion request found to cancel.");
        }

        if (request.Status == "Pending")
        {
            await _emailService.SendCancellationRequestToAdminPendingAsync(
              "techthrivers@gmail.com",
              user.FullName,
              user.Email,
              request.Id,
              request.RequestDate
          );
        }
        else if (request.Status == "Approved" && request.DeletionScheduledAt.HasValue)
        {
            // Case 2: Status was Approved and deletion is scheduled

            var timeRemaining = request.DeletionScheduledAt.Value.Subtract(DateTime.UtcNow);

            // Format time remaining for the admin email
            string timeString;
            if (timeRemaining.TotalHours >= 1)
            {
                timeString = $"approximately **{timeRemaining.TotalHours:F1} hours**";
            }
            else if (timeRemaining.TotalMinutes > 0)
            {
                timeString = $"approximately **{timeRemaining.TotalMinutes:F0} minutes**";
            }
            else
            {
                // Deletion time has already passed (handle immediately)
                timeString = "IMMEDIATELY (Scheduled time has passed)";
            }
            await _emailService.SendCancellationRequestToAdminApprovedAsync(
              "techthrivers@gmail.com",
              user.FullName,
              user.Email,
              request.Id,
              request.RequestDate,
              request.DeletionScheduledAt,
              timeString
          );
        }
        else
        {
            return BadRequest("No Request can be generated at this moment.");
        }
        request.Status = "Cancellation Requested";
        await _context.SaveChangesAsync();


        return Ok(new { message = "Deletion cancellation request has been submitted to the administration." });
    }

    [HttpGet("deletion/requests")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> GetDeletionRequests()
    {
        var requests = await _context.UserDeletionRequests
            .Include(r => r.User)
            .Select(r => new
            {
                r.Id,
                r.Status,
                r.RequestDate,
                r.DeletionScheduledAt,
                user = new
                {
                    r.User.Id,
                    r.User.FullName,
                    r.User.Email
                }
            })
            .OrderByDescending(r => r.RequestDate)
            .ToListAsync();

        return Ok(requests);
    }


    [HttpGet("notifications")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> GetNotifications()
    {
        var deletionRequests = await _context.UserDeletionRequests
            .Where(r => r.Status == "Pending")
            .Select(r => new
            {
                type = "deletion_request",
                message = $"{r.User.FullName} requested account deletion",
                date = r.RequestDate,
                seen = false
            })
            .ToListAsync();

        return Ok(deletionRequests);
    }

    [HttpPost("deletion/approve/{requestId}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ApproveDeletion(int requestId)
    {
        var request = await _context.UserDeletionRequests
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.Id == requestId);
        if (request == null) return NotFound();

        request.Status = "Approved";
        request.ApprovedDate = DateTime.UtcNow;
        request.DeletionScheduledAt = DateTime.UtcNow.AddHours(24);

        await _context.SaveChangesAsync();

        await _emailService.SendDeletionApprovedAsync(
            request.User.Email,
            request.User.FullName,
            request.DeletionScheduledAt.Value
        );

        return Ok(request);
    }

    [HttpPost("cancellation/approve/{requestId}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ApproveDeletionCancellation(int requestId)
    {
        var request = await _context.UserDeletionRequests
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.Id == requestId);

        if (request == null)
        {
            return NotFound("Deletion request not found.");
        }

       
        if (request.Status != "Cancellation Requested")
        {
            return BadRequest($"Cannot cancel request with current status: {request.Status}.");
        }
        request.Status = "Canceled";
        request.ApprovedDate = null;
        request.DeletionScheduledAt = null;

       
        await _context.SaveChangesAsync();

        // 5. Send confirmation email to the user
        await _emailService.SendCancellationApprovedToUserAsync(
            request.User.Email,
            request.User.FullName,
            request.Status
        );

        return Ok(new { message = $"Deletion request ID {requestId} successfully canceled. User account restored." });
    }
}