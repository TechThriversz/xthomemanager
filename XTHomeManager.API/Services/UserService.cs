using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using XTHomeManager.API.Data;
using XTHomeManager.API.Models;

namespace XTHomeManager.API.Services
{
    public class UserService
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;

        public UserService(AppDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        public async Task<User> RegisterAsync(string email, string fullName, string password, string role = "Admin")
        {
            var user = new User
            {
                Email = email,
                FullName = fullName,
                PasswordHash = HashPassword(password),
                Role = role
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return user;
        }

        public async Task<User> LoginAsync(string email, string password)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null || !VerifyPassword(password, user.PasswordHash))
            {
                Console.WriteLine($"Login failed for {email}: User not found or password mismatch");
                return null;
            }
            return user;
        }

        public async Task<User> InviteViewerAsync(string email, string fullName, string adminId, string recordName, string recordType)
        {
            var user = new User
            {
                Email = email,
                FullName = fullName,
                PasswordHash = HashPassword(GenerateRandomPassword()),
                Role = "Viewer",
                AdminId = adminId
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var record = await _context.Records.FirstOrDefaultAsync(r => r.Name == recordName && r.Type == recordType && r.UserId == adminId);
            if (record != null)
            {
                record.ViewerId = user.Id;
                record.AllowViewerAccess = true;
                await _context.SaveChangesAsync();
            }

            return user;
        }

        public async Task RevokeViewerAccessAsync(string viewerId, string recordName, string recordType)
        {
            var record = await _context.Records.FirstOrDefaultAsync(r => r.ViewerId == viewerId && r.Name == recordName && r.Type == recordType);
            if (record != null)
            {
                record.ViewerId = null;
                record.AllowViewerAccess = false;
                await _context.SaveChangesAsync();
            }
        }

        public string GenerateJwtToken(User user)
        {
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.Role, user.Role),
                new Claim("AdminId", user.AdminId ?? user.Id)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.Now.AddDays(1),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(bytes);
        }

        private bool VerifyPassword(string password, string hash)
        {
            var computedHash = HashPassword(password);
            Console.WriteLine($"Input Password: {password}, Computed Hash: {computedHash}, Stored Hash: {hash}");
            return computedHash == hash;
        }

        private string GenerateRandomPassword()
        {
            return Guid.NewGuid().ToString("N").Substring(0, 12);
        }
    }
}