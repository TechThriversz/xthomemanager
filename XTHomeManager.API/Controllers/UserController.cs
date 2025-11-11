using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using XTHomeManager.API.Data;
using XTHomeManager.API.Models;
using XTHomeManager.API.Services;
using System.IO;

namespace XTHomeManager.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly UserService _userService;
        private readonly AmazonS3Client _s3Client; 

        public UserController(AppDbContext context, UserService userService, AmazonS3Client s3Client)
        {
            _context = context;
            _userService = userService;
            _s3Client = s3Client ?? throw new ArgumentNullException(nameof(s3Client), "S3 client is not initialized.");
        }

        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> UpdateUser(string id, [FromForm] UpdateUserModel model)
        {
            try
            {
                var user = await _context.Users.FindAsync(id);
                if (user == null)
                {
                    return NotFound("User not found");
                }

                var authUserId = User.FindFirst("id")?.Value;
                if (authUserId != id)
                {
                    return Forbid("You can only update your own profile");
                }

                if (!string.IsNullOrEmpty(model.FullName))
                {
                    user.FullName = model.FullName;
                }

                if (!string.IsNullOrEmpty(model.PhoneNumber))
                {
                    user.PhoneNumber = model.PhoneNumber;
                }

                if (!string.IsNullOrEmpty(model.Password))
                {
                    user.PasswordHash = _userService.HashPassword(model.Password);
                }
                if (model.Image != null)
                {
                    var fileName = $"{Guid.NewGuid()}{Path.GetExtension(model.Image.FileName)}";
                    var uploadRequest = new PutObjectRequest
                    {
                        BucketName = "xthomemanager-uploads", // Match your R2 bucket name
                        Key = fileName,
                        ContentType = model.Image.ContentType,
                        DisablePayloadSigning = true // Attempt to disable payload signing
                    };

                    // Use a MemoryStream and upload without disposing early
                    using (var memoryStream = new MemoryStream())
                    {
                        await model.Image.CopyToAsync(memoryStream);
                        memoryStream.Position = 0; // Reset to beginning
                        uploadRequest.InputStream = memoryStream;

                        // Perform the upload
                        await _s3Client.PutObjectAsync(uploadRequest);
                    }

                    user.ImagePath = fileName; // Store only the filename
                }

                await _context.SaveChangesAsync();
                return Ok(new
                {
                    user.Id,
                    user.Email,
                    user.Role,
                    user.FullName,
                    user.ImagePath,
                    user.IsActive,
                    user.IsPro,
                    user.CanUsePasswordVault,
                    user.CanUseFamilyMembers,
                    user.CanUseMedicalRecords,
                    user.PhoneNumber

                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred while updating the user: {ex.ToString()}"); // Full exception details
            }
        }

        [HttpGet("me")]
        public async Task<ActionResult> GetCurrentUser()
        {
            var userId = User.FindFirst("id")?.Value;
            var user = await _context.Users
                .Include(u => u.ProUpgradeRequests)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null) return NotFound();

            return Ok(new
            {
                user.Id,
                user.FullName,
                user.Email,
                user.PhoneNumber,
                user.Role,
                user.ImagePath,
                user.IsActive,
                user.IsPro,
                user.ProEndDate,
                user.CanUsePasswordVault,
                user.CanUseFamilyMembers,
                user.CanUseMedicalRecords,
                proUpgradeRequests = user.ProUpgradeRequests
                    .Select(r => new { r.Id, r.Status, r.RequestDate })
                    .ToList()
            });
        }
    }



    public class UpdateUserModel
    {
        public string? FullName { get; set; }

        public string? PhoneNumber { get; set; }
        public string? Password { get; set; }
        public IFormFile? Image { get; set; }
    }
}