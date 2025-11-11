// Controllers/FamilyController.cs
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using XTHomeManager.API.Data;
using XTHomeManager.API.Models;

[Route("api/family")]
[ApiController]
[Authorize]
public class FamilyController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly AmazonS3Client _s3Client;

    public FamilyController(AppDbContext context, AmazonS3Client s3Client)
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

    [HttpGet("tree")]
    public async Task<ActionResult> GetTree()
    {
        var userId = User.FindFirst("id")?.Value;
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        var rawMembers = await _context.FamilyMembers
            .Where(m => m.UserId == userId)
            .Select(m => new
            {
                m.Id,
                m.Name,
                m.Birthday,
                m.Relation,
                m.ParentIds,
                m.SpouseIds,
                m.IsDeceased,
                m.DeathDate,
                m.BornPlace,
                m.DiedPlace,
                m.ImagePath
            })
            .ToListAsync();

        var members = rawMembers.Select(m => new
        {
            m.Id,
            m.Name,
            m.Birthday,
            m.Relation,
            ParentIds = TryDeserialize(m.ParentIds),
            SpouseIds = TryDeserialize(m.SpouseIds),
            m.IsDeceased,
            m.DeathDate,
            m.BornPlace,
            m.DiedPlace,
            m.ImagePath
        }).ToList();

        return Ok(members);
    }

    private static List<int> TryDeserialize(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return new List<int>();
        try
        {
            return JsonSerializer.Deserialize<List<int>>(json) ?? new List<int>();
        }
        catch
        {
            return new List<int>();
        }
    }

    [HttpPost("tree")]
    public async Task<ActionResult> Add([FromForm] FamilyMemberDto dto)
    {
        var userId = User.FindFirst("id")?.Value;
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        var imagePath = await UploadToR2(dto.Image);

        var parentIds = string.IsNullOrWhiteSpace(dto.ParentIdsJson)
            ? "[]"
            : dto.ParentIdsJson;

        var spouseIds = string.IsNullOrWhiteSpace(dto.SpouseIdsJson)
            ? "[]"
            : dto.SpouseIdsJson;

        var member = new FamilyMember
        {
            UserId = userId,
            Name = dto.Name,
            Birthday = dto.Birthday,
            Relation = dto.Relation,
            ParentIds = parentIds,
            SpouseIds = spouseIds,
            IsDeceased = dto.IsDeceased,
            DeathDate = dto.DeathDate,
            BornPlace = dto.BornPlace,
            DiedPlace = dto.DiedPlace,
            ImagePath = imagePath
        };

        _context.FamilyMembers.Add(member);
        await _context.SaveChangesAsync();
        return Ok(new { member.Id });
    }

    [HttpPut("tree/{id}")]
    public async Task<ActionResult> Update(int id, [FromForm] FamilyMemberDto dto)
    {
        var userId = User.FindFirst("id")?.Value;
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        var member = await _context.FamilyMembers
            .FirstOrDefaultAsync(m => m.Id == id && m.UserId == userId);
        if (member == null) return NotFound();

        if (dto.Image != null)
            member.ImagePath = await UploadToR2(dto.Image);

        member.Name = dto.Name;
        member.Birthday = dto.Birthday;
        member.Relation = dto.Relation;
        member.ParentIds = string.IsNullOrWhiteSpace(dto.ParentIdsJson) ? "[]" : dto.ParentIdsJson;
        member.SpouseIds = string.IsNullOrWhiteSpace(dto.SpouseIdsJson) ? "[]" : dto.SpouseIdsJson;
        member.IsDeceased = dto.IsDeceased;
        member.DeathDate = dto.DeathDate;
        member.BornPlace = dto.BornPlace;
        member.DiedPlace = dto.DiedPlace;

        await _context.SaveChangesAsync();
        return Ok();
    }

    [HttpDelete("tree/{id}")]
    public async Task<ActionResult> Delete(int id)
    {
        var userId = User.FindFirst("id")?.Value;
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        var member = await _context.FamilyMembers
            .FirstOrDefaultAsync(m => m.Id == id && m.UserId == userId);

        if (member == null) return NotFound();

        _context.FamilyMembers.Remove(member);
        await _context.SaveChangesAsync();
        return Ok();
    }
}