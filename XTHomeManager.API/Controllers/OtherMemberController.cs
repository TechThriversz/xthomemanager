// Controllers/OtherMemberController.cs
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using XTHomeManager.API.Data;
using XTHomeManager.API.Models;

[Route("api/othermember")]
[ApiController]
[Authorize]
public class OtherMemberController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly AmazonS3Client _s3Client;

    public OtherMemberController(AppDbContext context, AmazonS3Client s3Client)
    {
        _context = context;
        _s3Client = s3Client;
    }

    private async Task<string?> UploadToR2(IFormFile? file)
    {
        if (file == null || file.Length == 0) return null;

        var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
        var request = new PutObjectRequest
        {
            BucketName = "xthomemanager-uploads",
            Key = fileName,
            ContentType = file.ContentType,
            InputStream = file.OpenReadStream(),
            DisablePayloadSigning = true
        };

        await _s3Client.PutObjectAsync(request);
        return fileName;
    }

    [HttpGet]
    public async Task<ActionResult> GetAll()
    {
        var userId = User.FindFirst("id")?.Value;
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        var members = await _context.OtherMembers
            .Where(m => m.UserId == userId) // No ?? here → EF Core safe
            .Select(m => new
            {
                m.Id,
                m.Name,
                m.Relation,
                m.Birthday,
                m.IsDeceased,
                m.DeathDate,
                m.BornPlace,
                m.DiedPlace,
                m.ImagePath
            })
            .ToListAsync();

        return Ok(members);
    }

    [HttpPost]
    public async Task<ActionResult> Add([FromForm] OtherMemberDto dto)
    {
        var userId = User.FindFirst("id")?.Value;
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        var imagePath = await UploadToR2(dto.Image);

        var member = new OtherMember
        {
            UserId = userId,
            Name = dto.Name,
            Relation = dto.Relation,
            Birthday = dto.Birthday,
            IsDeceased = dto.IsDeceased,
            DeathDate = dto.DeathDate,
            BornPlace = dto.BornPlace,
            DiedPlace = dto.DiedPlace,
            ImagePath = imagePath
        };

        _context.OtherMembers.Add(member);
        await _context.SaveChangesAsync();
        return Ok(new { member.Id });
    }

    [HttpPut("{id}")]
    public async Task<ActionResult> Update(int id, [FromForm] OtherMemberDto dto)
    {
        var userId = User.FindFirst("id")?.Value;
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        var member = await _context.OtherMembers
            .FirstOrDefaultAsync(m => m.Id == id && m.UserId == userId);

        if (member == null) return NotFound();

        if (dto.Image != null)
            member.ImagePath = await UploadToR2(dto.Image);

        member.Name = dto.Name;
        member.Relation = dto.Relation;
        member.Birthday = dto.Birthday;
        member.IsDeceased = dto.IsDeceased;
        member.DeathDate = dto.DeathDate;
        member.BornPlace = dto.BornPlace;
        member.DiedPlace = dto.DiedPlace;

        await _context.SaveChangesAsync();
        return Ok();
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(int id)
    {
        var userId = User.FindFirst("id")?.Value;
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        var member = await _context.OtherMembers
            .FirstOrDefaultAsync(m => m.Id == id && m.UserId == userId);

        if (member == null) return NotFound();

        _context.OtherMembers.Remove(member);
        await _context.SaveChangesAsync();
        return Ok();
    }
}