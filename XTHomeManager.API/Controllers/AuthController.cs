// AuthController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;
using XTHomeManager.API.Models;
using XTHomeManager.API.Services;

namespace XTHomeManager.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly UserService _userService;
        private readonly EmailService _emailService;
        private readonly IConfiguration _configuration;

        public AuthController(UserService userService, EmailService emailService, IConfiguration configuration)
        {
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));
            _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<ActionResult> Login([FromBody] LoginModel model)
        {
            if (model == null || string.IsNullOrEmpty(model.Email) || string.IsNullOrEmpty(model.Password))
                return BadRequest("Email and password are required.");

            var user = await _userService.GetUserByEmailAsync(model.Email);
            if (user == null) return Unauthorized("User not found. If you had account please contact at techthrivers@gmail.com");
            if (user == null || !_userService.VerifyPassword(model.Password, user.PasswordHash))
            {
                return Unauthorized("Invalid email or password");
            }
            if (user == null || !user.IsActive) 
                return Unauthorized("Account is deactivated. Contact techthrivers@gmail.com");

            var token = _userService.GenerateJwtToken(user);
            return Ok(new { Token = token, User = new { user.Id, user.Email, user.FullName, user.ImagePath, user.Role,user.IsActive } });
        }

        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<ActionResult> Register([FromBody] RegisterModel model)
        {
            if (model == null || string.IsNullOrEmpty(model.Email) || string.IsNullOrEmpty(model.FullName) || string.IsNullOrEmpty(model.Password))
                return BadRequest("All fields are required.");

            var user = await _userService.RegisterAsync(model.Email, model.FullName, model.Password);
            if (user == null)
                return BadRequest("User with this email already exists.");

            try
            {
                await _emailService.SendWelcomeEmailAsync(user.Email, user.FullName);
            }
            catch (Exception ex)
            {
                // LOG BUT DON'T CRASH
                Console.WriteLine($"Welcome email failed: {ex.Message}");
            }

            var token = _userService.GenerateJwtToken(user);
            return Ok(new { Token = token, User = new { user.Id, user.Email, user.FullName, user.ImagePath, user.Role, user.IsActive } });
        }

        [HttpPost("forgot-password")]
        public async Task<ActionResult> ForgotPassword([FromBody] ForgotPasswordModel model)
        {
            if (model == null || string.IsNullOrEmpty(model.Email))
                return BadRequest("Email is required.");

            var (user, token) = await _userService.GeneratePasswordResetTokenAsync(model.Email);
            if (user == null || string.IsNullOrEmpty(token))
            {
                return Ok("If an account exists, a reset link has been sent.");
            }

            var frontendBaseUrl = _configuration["Frontend:BaseUrl"] ?? "https://xthomemanager.vercel.app";
            var resetLink = $"{frontendBaseUrl}/reset-password?token={token}&email={user.Email}";
            // REMOVE Uri.EscapeDataString → LET BROWSER HANDLE IT

            try
            {
                await _emailService.SendPasswordResetEmailAsync(user.Email, user.FullName, resetLink);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Reset email failed: {ex.Message}");
                return StatusCode(500, "Failed to send reset email. Try again later.");
            }

            return Ok("Reset link sent if account exists.");
        }
        [HttpPost("reset-password")]
        public async Task<ActionResult> ResetPassword([FromBody] ResetPasswordModel model)
        {
            if (model == null || string.IsNullOrEmpty(model.Email) || string.IsNullOrEmpty(model.Token) || string.IsNullOrEmpty(model.NewPassword))
                return BadRequest("Email, token, and new password are required.");

            var success = await _userService.ResetPasswordAsync(model.Email, model.Token, model.NewPassword);
            if (!success)
                return BadRequest("Invalid token or email. Please try again or request a new reset link.");

            return Ok("Password has been reset successfully.");
        }

        [HttpPost("invite")]
        [Authorize]
        public async Task<ActionResult> InviteViewer([FromBody] InviteModel model)
        {
            var userId = User.FindFirst("id")?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { Message = "Invalid token." });

            var admin = await _userService.GetUserByIdAsync(userId);
            if (admin == null)
                return NotFound(new { Message = "Admin not found." });

            var (user, message) = await _userService.InviteOrUpdateViewerAsync(
                model.Email, admin.FullName, userId, model.RecordName, model.RecordId);

            if (user == null)
                return BadRequest(new { Message = message });

            var isNewUser = message.Contains("temporary password");
            var tempPassword = isNewUser ? message.Split("temporary password: ")[1] : null;

            await _emailService.SendInviteEmailAsync(
                user.Email,
                user.FullName ?? user.Email.Split('@')[0],
                admin.FullName,
                model.RecordName,
                tempPassword
            );

            return Ok(new
            {
                Message = isNewUser
                    ? "New viewer invited. Temporary password sent."
                    : $"Viewer {model.Email} added. They can now accept the invite."
            });
        }

        [HttpPost("revoke")]
        [Authorize]
        public async Task<ActionResult> RevokeViewer([FromBody] RevokeModel model)
        {
            var userId = User.FindFirst("id")?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized("User ID not found in token.");

            await _userService.RevokeViewerAccessAsync(model.ViewerId, model.RecordId);
            // Fetch viewer email to send notification
            var viewer = await _userService.GetUserByIdAsync(model.ViewerId);
            if (viewer != null)
            {
                await _emailService.SendRevokeEmailAsync(viewer.Email, viewer.FullName, model.RecordId.ToString());
            }
            return Ok(new { Message = "Viewer access revoked successfully" });
        }

        [HttpGet("invited-viewers/{adminId}")]
        public async Task<ActionResult<List<InvitedViewerDto>>> GetInvitedViewers(string adminId)
        {
            var viewers = await _userService.GetInvitedViewersAsync(adminId);
            return Ok(viewers);
        }
    }
}