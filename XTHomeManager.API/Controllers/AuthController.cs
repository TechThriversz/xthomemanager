using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using XTHomeManager.API.Models;
using XTHomeManager.API.Services;

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

        [HttpPost("login")]
        public async Task<ActionResult> Login([FromBody] LoginModel model)
        {
            var user = await _userService.LoginAsync(model.Email, model.Password);
            if (user == null)
                return Unauthorized("Invalid email or password");

            var token = _userService.GenerateJwtToken(user);
            return Ok(new { Token = token, User = new { user.Email, user.Role, user.FullName, user.Id } });
        }

        [HttpPost("register")]
        public async Task<ActionResult<User>> Register([FromBody] RegisterModel model)
        {
            var user = await _userService.RegisterAsync(model.Email, model.FullName, model.Password);
            return Ok(user);
        }

        [HttpPost("invite")]
        public async Task<ActionResult<User>> Invite([FromBody] InviteModel model)
        {
            var adminId = User.FindFirst("AdminId")?.Value;
            if (adminId == null)
                return Unauthorized();

            var user = await _userService.InviteViewerAsync(model.Email, model.FullName, adminId, model.RecordName, model.RecordType);
            return Ok(user);
        }

        [HttpPost("revoke")]
        public async Task<ActionResult> RevokeViewer([FromBody] RevokeModel model)
        {
            var adminId = User.FindFirst("AdminId")?.Value;
            if (adminId == null)
                return Unauthorized();

            await _userService.RevokeViewerAccessAsync(model.ViewerId, model.RecordName, model.RecordType);
            return Ok();
        }
    }
}