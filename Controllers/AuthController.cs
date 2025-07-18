using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using XTHomeManager.API.Data;
using XTHomeManager.API.Services;
using XTHomeManager.API.Models;

namespace XTHomeManager.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly UserService _userService;

        public AuthController(UserService userService)
        {
            _userService = userService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterModel model)
        {
            var user = await _userService.RegisterAsync(model.Email, model.Password);
            if (user == null) return BadRequest("Registration failed");
            return Ok();
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginModel model)
        {
            var user = await _userService.LoginAsync(model.Email, model.Password);
            if (user == null) return Unauthorized();
            var token = _userService.GenerateJwtToken(user);
            return Ok(new { Token = token });
        }

        [Authorize(Roles = "Admin")]
        [HttpPost("invite")]
        public async Task<IActionResult> Invite([FromBody] InviteModel model)
        {
            var adminId = User.FindFirst("AdminId")?.Value;
            var user = await _userService.InviteViewerAsync(model.Email, adminId);
            if (user == null) return BadRequest("Invite failed");
            return Ok(new { Email = user.Email, TemporaryPassword = user.PasswordHash });
        }
    }

    public class RegisterModel
    {
        public string Email { get; set; }
        public string Password { get; set; }
    }

    public class LoginModel
    {
        public string Email { get; set; }
        public string Password { get; set; }
    }

    public class InviteModel
    {
        public string Email { get; set; }
    }
}