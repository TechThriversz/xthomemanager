using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using XTHomeManager.API.Data;
using XTHomeManager.API.Services;
using Microsoft.EntityFrameworkCore;

namespace XTHomeManager.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly UserService _userService;

        public UserController(AppDbContext context, UserService userService)
        {
            _context = context;
            _userService = userService;
        }

        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> UpdateUser(string id, [FromForm] string fullName, [FromForm] string? password, [FromForm] IFormFile? image)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();
            user.FullName = fullName;
            if (!string.IsNullOrEmpty(password)) user.PasswordHash = _userService.HashPassword(password);
            if (image != null)
            {
                var uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "Uploads");
                if (!Directory.Exists(uploadsDir)) Directory.CreateDirectory(uploadsDir);
                var filePath = Path.Combine(uploadsDir, $"{Guid.NewGuid()}{Path.GetExtension(image.FileName)}");
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await image.CopyToAsync(stream);
                }
                user.ImagePath = filePath;
            }
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}