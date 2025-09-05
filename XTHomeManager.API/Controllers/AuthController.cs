// AuthController.cs
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
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

        public AuthController(UserService userService, EmailService emailService)
        {
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));
            _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
        }

        [HttpPost("login")]
        public async Task<ActionResult> Login([FromBody] LoginModel model)
        {
            if (model == null || string.IsNullOrEmpty(model.Email) || string.IsNullOrEmpty(model.Password))
                return BadRequest(new { Message = "Email and password are required." });

            var user = await _userService.LoginAsync(model.Email, model.Password);
            if (user == null)
            {
                return Unauthorized(new { Message = "Invalid email or password" });
            }
            if (user.Role == "Viewer" && user.PasswordResetTokenExpiry.HasValue && user.PasswordResetTokenExpiry.Value > DateTime.UtcNow)
            {
                var token = _userService.GenerateJwtToken(user);
                return Ok(new { Message = "Please change your temporary password.", RequiresPasswordChange = true, Token = token });
            }

            var tokenResult = _userService.GenerateJwtToken(user);
            return Ok(new { Message = "Login successful", Token = tokenResult, User = new { user.Id, user.Email, user.Role, user.FullName, user.ImagePath } });
        }

        [HttpPost("register")]
        public async Task<ActionResult> Register([FromBody] RegisterModel model)
        {
            if (model == null || string.IsNullOrEmpty(model.Email) || string.IsNullOrEmpty(model.FullName) || string.IsNullOrEmpty(model.Password))
                return BadRequest(new { Message = "All fields are required." });

            var existingUser = await _userService.GetUserByEmailAsync(model.Email);
            string token;

            if (existingUser != null)
            {
                if (existingUser.Role == "Viewer")
                {
                    existingUser.FullName = model.FullName;
                    existingUser.PasswordHash = _userService.HashPassword(model.Password);
                    await _userService.ClearTemporaryPasswordAsync(existingUser.Id);
                    await _userService.SaveChangesAsync();
                    await _emailService.SendWelcomeEmailAsync(existingUser.Email, existingUser.FullName);
                    token = _userService.GenerateJwtToken(existingUser);
                    return Ok(new { Message = "Viewer updated successfully", Token = token, User = existingUser });
                }
                return BadRequest(new { Message = "User with this email already exists and is not a viewer." });
            }

            var user = await _userService.RegisterAsync(model.Email, model.FullName, model.Password);
            if (user == null)
                return BadRequest(new { Message = "User with this email already exists." });

            await _emailService.SendWelcomeEmailAsync(user.Email, user.FullName);
            token = _userService.GenerateJwtToken(user);
            return Ok(new { Message = "Registration successful", Token = token, User = user });
        }

        [HttpPost("forgot-password")]
        public async Task<ActionResult> ForgotPassword([FromBody] ForgotPasswordModel model)
        {
            if (model == null || string.IsNullOrEmpty(model.Email))
                return BadRequest("Email is required.");

            var (user, token) = await _userService.GeneratePasswordResetTokenAsync(model.Email);
            if (user == null || string.IsNullOrEmpty(token))
            {
                return Ok(new { message = "If an account with that email exists, a password reset link has been sent." });
            }

            var request = HttpContext.Request;
            var resetLink = $"{request.Scheme}://{request.Host}/reset-password?token={token}&email={Uri.EscapeDataString(user.Email)}";
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

            return Ok(new { message = "Password has been reset successfully." });
        }

        [HttpPost("invite")]
        public async Task<ActionResult<object>> Invite([FromBody] InviteModel model)
        {
            var adminId = User.FindFirst("AdminId")?.Value;
            if (string.IsNullOrEmpty(adminId)) return Unauthorized(new { Message = "Unauthorized access" });

            var admin = await _userService.GetUserByIdAsync(adminId);
            if (admin == null) return BadRequest(new { Message = "Admin not found." });

            var tempPassword = _userService.GetTemporaryPassword();
            var result = await _userService.InviteOrUpdateViewerAsync(model.Email, admin.FullName, adminId, model.RecordName, tempPassword);
            if (result.Item1 != null)
            {
                await _emailService.SendInviteEmailAsync(result.Item1.Email, result.Item1.FullName, admin.FullName, model.RecordName, tempPassword);
            }
            return Ok(new { User = result.Item1, Message = result.Item2 });
        }

        [HttpPost("revoke")]
        public async Task<ActionResult> RevokeViewer([FromBody] RevokeModel model)
        {
            var adminId = User.FindFirst("AdminId")?.Value;
            if (string.IsNullOrEmpty(adminId)) return Unauthorized(new { Message = "Unauthorized access" });

            await _userService.RevokeViewerAccessAsync(model.ViewerId, model.RecordName, model.RecordType);
            return Ok(new { Message = "Viewer access revoked successfully" });
        }

        [HttpGet("invited-viewers/{adminId}")]
        public async Task<ActionResult<List<User>>> GetInvitedViewers(string adminId)
        {
            var viewers = await _userService.GetInvitedViewersAsync(adminId);
            return Ok(viewers);
        }
    }
}