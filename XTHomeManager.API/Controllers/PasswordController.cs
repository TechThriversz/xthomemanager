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
    public PasswordController(PasswordService service) => _service = service;

    private string UserId => User.FindFirst("id")?.Value
                             ?? throw new UnauthorizedAccessException("User not found");

    [HttpGet]
    public async Task<ActionResult<List<Password>>> Get()
    {
        var passwords = await _service.GetPasswordsAsync(UserId);
        var decrypted = passwords.Select(p => new
        {
            p.Id,
            p.AccountName,
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
        var password = await _service.AddPasswordAsync(UserId, dto);
        return CreatedAtAction(nameof(GetById), new { id = password.Id }, password);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Password>> GetById(int id)
    {
        var password = await _service.GetPasswordByIdAsync(id, UserId);
        return password == null ? NotFound() : Ok(password);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] PasswordDto dto)
    {
        var result = await _service.UpdatePasswordAsync(id, UserId, dto);
        return result != null ? Ok(result) : NotFound();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _service.DeletePasswordAsync(id, UserId);
        return result ? Ok() : NotFound();
    }
}