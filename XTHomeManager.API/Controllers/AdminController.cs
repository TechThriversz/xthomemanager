// Controllers/AdminController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using XTHomeManager.API.Data;
using XTHomeManager.API.Models;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class AdminController : ControllerBase
{
    private readonly AppDbContext _context;

    public AdminController(AppDbContext context) => _context = context;

    [HttpGet("users")]
    public async Task<ActionResult> GetUsers()
    {
        var users = await _context.Users
            .Select(u => new
            {
                u.Id,
                u.FullName,
                u.Email,
                u.Role,
                u.IsActive
            })
            .ToListAsync();

        return Ok(users);
    }

    [HttpPatch("users/{id}/toggle")]
    public async Task<IActionResult> ToggleUser(string id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null) return NotFound();
        user.IsActive = !user.IsActive;
        await _context.SaveChangesAsync();
        return Ok(new { user.Id, user.IsActive });
    }

    [HttpPatch("users/{id}/role")]
    public async Task<IActionResult> SetRole(string id, [FromBody] string role)
    {
        if (!new[] { "Admin", "User" }.Contains(role)) return BadRequest("Invalid role");
        var user = await _context.Users.FindAsync(id);
        if (user == null) return NotFound();
        user.Role = role;
        await _context.SaveChangesAsync();
        return Ok();
    }
}