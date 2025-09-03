// AuthController.cs
using Microsoft.AspNetCore.Mvc;
using XTHomeManager.API.Models;
using XTHomeManager.API.Services;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

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
                return BadRequest("Email and password are required.");

            var user = await _userService.LoginAsync(model.Email, model.Password);
            if (user == null)
                return Unauthorized("Invalid email or password");

            var token = _userService.GenerateJwtToken(user);
            return Ok(new
            {
                Token = token,
                User = new { user.Id, user.Email, user.Role, user.FullName, user.ImagePath }
            });
        }

        [HttpPost("register")]
        public async Task<ActionResult> Register([FromBody] RegisterModel model)
        {
            if (model == null || string.IsNullOrEmpty(model.Email) || string.IsNullOrEmpty(model.FullName) || string.IsNullOrEmpty(model.Password))
                return BadRequest("All fields are required.");

            var user = await _userService.RegisterAsync(model.Email, model.FullName, model.Password);
            if (user == null)
                return BadRequest("User with this email already exists.");

            await _emailService.SendWelcomeEmailAsync(user.Email, user.FullName);
            var token = _userService.GenerateJwtToken(user);

            return Ok(new
            {
                Token = token,
                User = user
            });
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
        public async Task<ActionResult<XTHomeManager.API.Models.User>> Invite([FromBody] InviteModel model)
        {
            var adminId = User.FindFirst("AdminId")?.Value;
            if (string.IsNullOrEmpty(adminId)) return Unauthorized();

            var user = await _userService.InviteViewerAsync(model.Email, adminId, model.RecordName);
            if (user != null)
            {
              //  await _emailService.SendInviteEmailAsync(user.Email, user.FullName, adminId, model.RecordName);
            }
            return Ok(user);
        }

        [HttpPost("revoke")]
        public async Task<ActionResult> RevokeViewer([FromBody] RevokeModel model)
        {
            var adminId = User.FindFirst("AdminId")?.Value;
            if (string.IsNullOrEmpty(adminId)) return Unauthorized();

            await _userService.RevokeViewerAccessAsync(model.ViewerId, model.RecordName, model.RecordType);
            return Ok();
        }
    }
}