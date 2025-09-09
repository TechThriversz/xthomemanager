// AuthController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
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
        [Microsoft.AspNetCore.Authorization.AllowAnonymous]
        public async Task<ActionResult> Login([FromBody] LoginModel model)
        {
            if (model == null || string.IsNullOrEmpty(model.Email) || string.IsNullOrEmpty(model.Password))
                return BadRequest("Email and password are required.");

            var user = await _userService.LoginAsync(model.Email, model.Password);
            if (user == null)
            {
                return Unauthorized("Invalid email or password");
            }

            if (user.PasswordResetTokenExpiry.HasValue && user.PasswordResetTokenExpiry.Value > DateTime.UtcNow)
            {
                var token = _userService.GenerateJwtToken(user);
                return Ok(new { Token = token, RequiresPasswordChange = true });
            }

            var tokenResult = _userService.GenerateJwtToken(user);
            return Ok(new { Token = tokenResult, User = new { user.Id, user.Email, user.Role, user.FullName, user.ImagePath } });
        }

        [HttpPost("register")]
        [Microsoft.AspNetCore.Authorization.AllowAnonymous]
        public async Task<ActionResult> Register([FromBody] RegisterModel model)
        {
            if (model == null || string.IsNullOrEmpty(model.Email) || string.IsNullOrEmpty(model.FullName) || string.IsNullOrEmpty(model.Password))
                return BadRequest("All fields are required.");

            var user = await _userService.RegisterAsync(model.Email, model.FullName, model.Password);
            if (user == null)
                return BadRequest("User with this email already exists.");

            await _emailService.SendWelcomeEmailAsync(user.Email, user.FullName);
            var token = _userService.GenerateJwtToken(user);

            return Ok(new { Token = token, User = user });
        }

        [HttpPost("forgot-password")]
        public async Task<ActionResult> ForgotPassword([FromBody] ForgotPasswordModel model)
        {
            if (model == null || string.IsNullOrEmpty(model.Email))
                return BadRequest("Email is required.");

            var (user, token) = await _userService.GeneratePasswordResetTokenAsync(model.Email);
            if (user == null || string.IsNullOrEmpty(token))
            {
                return Ok("If an account with that email exists, a password reset link has been sent.");
            }

            // Use the configured frontend URL from appsettings.json
            var frontendBaseUrl = _configuration["Frontend:BaseUrl"] ?? "http://localhost:5173"; // Default to localhost if not set
            var resetLink = $"{frontendBaseUrl}/reset-password?token={token}&email={Uri.EscapeDataString(user.Email)}";
            await _emailService.SendPasswordResetEmailAsync(user.Email, user.FullName, resetLink);

            return Ok(new { message = "If an account with that email exists, a password reset link has been sent." });
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

        //[HttpPost("invite")]
        //public async Task<ActionResult<User>> Invite([FromBody] InviteModel model)
        //{
        //    var adminId = User.FindFirst("AdminId")?.Value;
        //    if (string.IsNullOrEmpty(adminId)) return Unauthorized();

        //    var admin = await _userService.GetUserByIdAsync(adminId);
        //    if (admin == null) return BadRequest("Admin not found.");

        //    var (user, message) = await _userService.InviteOrUpdateViewerAsync(model.Email, admin.FullName, adminId, model.RecordName);
        //    if (user == null)
        //        return BadRequest(message);

        //    await _emailService.SendInviteEmailAsync(user.Email, user.FullName, admin.FullName, model.RecordName, message.Contains("temporary password") ? message.Split("temporary password: ")[1] : null);

        //    return Ok(new { User = user, Message = message });
        //}

        [HttpPost("invite")]
        [Authorize]
        public async Task<ActionResult> InviteViewer([FromBody] InviteModel model)
        {
            var userId = User.FindFirst("id")?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { Message = "User ID not found" });

            var admin = await _userService.GetUserByIdAsync(userId);
            if (admin == null)
                return BadRequest(new { Message = "Admin not found" });

            // Use UserService to handle the invite logic
            var (user, message) = await _userService.InviteOrUpdateViewerAsync(model.Email, admin.FullName, userId, model.RecordName);
            if (user == null)
                return BadRequest(new { Message = message });

            await _emailService.SendInviteEmailAsync(user.Email, user.FullName, admin.FullName, model.RecordName, message.Contains("temporary password") ? message.Split("temporary password: ")[1] : null);
            return Ok(new { Message = "Invitation sent successfully" });
        }

        [HttpPost("revoke")]
        [Authorize]
        public async Task<ActionResult> RevokeViewer([FromBody] RevokeModel model)
        {
            var userId = User.FindFirst("id")?.Value; // Use "id" instead of "AdminId" for consistency
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