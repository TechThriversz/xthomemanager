// Controllers/PasswordController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using XTHomeManager.API.Data;
using XTHomeManager.API.DTOs;
using XTHomeManager.API.Models;
using XTHomeManager.API.Services;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PasswordController : ControllerBase
{
    private readonly PasswordService _service;
    private readonly UserService _userService;  
    public PasswordController(PasswordService service, UserService userService)
    {
    _service = service ?? throw new ArgumentNullException(nameof(service));
    _userService = userService ?? throw new ArgumentNullException(nameof(userService));
        }

    private string UserId => User.FindFirst("id")?.Value
                             ?? throw new UnauthorizedAccessException("User not found");

    [HttpGet]
    public async Task<ActionResult<List<Password>>> Get()
    {
        var user = await _userService.GetUserByIdAsync(UserId);
        if (user == null) return NotFound("User not found");
        if (!user.CanUsePasswordVault) return Forbid("Access denied to Password Vault");

        var passwords = await _service.GetPasswordsAsync(UserId);
        var decrypted = passwords.Select(p => new
        {
            p.Id,
            p.AccountName,
            p.Url,
            p.Category,
            p.Email,
            p.Username,
            DecryptedPassword = _service.DecryptPassword(p.EncryptedPassword),
            p.SecurityMethod,
            p.SecurityValue,
            p.AssociatedPhone,
            p.RecoveryEmail,
            SecurityQuestions = string.IsNullOrEmpty(p.SecurityQuestions)
                ? null
                : System.Text.Json.JsonSerializer.Deserialize<List<SecurityQuestion>>(p.SecurityQuestions)
        }).ToList();
        return Ok(decrypted);
    }

    [HttpPost]
    public async Task<ActionResult<Password>> Create([FromBody] PasswordDto dto)
    {
        var user = await _userService.GetUserByIdAsync(UserId);
        if (user == null || !user.CanUsePasswordVault)
            return Forbid("Access denied to Password Vault");

        var password = await _service.AddPasswordAsync(UserId, dto);
        return CreatedAtAction(nameof(GetById), new { id = password.Id }, password);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Password>> GetById(int id)
    {
        var user = await _userService.GetUserByIdAsync(UserId);
        if (user == null || !user.CanUsePasswordVault)
            return Forbid("Access denied to Password Vault");
        var password = await _service.GetPasswordByIdAsync(id, UserId);
        return password == null ? NotFound() : Ok(password);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] PasswordDto dto)
    {
        var user = await _userService.GetUserByIdAsync(UserId);
        if (user == null || !user.CanUsePasswordVault)
            return Forbid("Access denied to Password Vault");
        var result = await _service.UpdatePasswordAsync(id, UserId, dto);
        return result != null ? Ok(result) : NotFound();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var user = await _userService.GetUserByIdAsync(UserId);
        if (user == null || !user.CanUsePasswordVault)
            return Forbid("Access denied to Password Vault");
        var result = await _service.DeletePasswordAsync(id, UserId);
        return result ? Ok() : NotFound();
    }
}