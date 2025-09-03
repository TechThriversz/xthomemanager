// UserService.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
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
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        public async Task<User> RegisterAsync(string email, string fullName, string password, string role = "Admin")
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(fullName) || string.IsNullOrEmpty(password))
                throw new ArgumentNullException("Email, full name, and password are required.");

            var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (existingUser != null) return null;

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
            try
            {
                var user = await _context.Users
                    .Where(u => u.Email == email)
                    .FirstOrDefaultAsync();

                if (user == null || string.IsNullOrEmpty(user.PasswordHash) || !VerifyPassword(password, user.PasswordHash))
                    return null;

                return user;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Login failed for {email}: {ex.Message}");
                return null;
            }
        }

        public async Task<User> InviteViewerAsync(string email, string adminId, string recordName)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(adminId))
                throw new ArgumentNullException("Email and adminId are required.");

            var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (existingUser != null) throw new InvalidOperationException("User with this email already exists.");

            var user = new User
            {
                Email = email,
                FullName = email.Split('@')[0], // Auto-generate full name from email
                PasswordHash = HashPassword(GenerateRandomPassword()),
                Role = "Viewer",
                AdminId = adminId
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var record = await _context.Records.FirstOrDefaultAsync(r => r.Name == recordName && r.UserId == adminId);
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
            if (string.IsNullOrEmpty(viewerId) || string.IsNullOrEmpty(recordName) || string.IsNullOrEmpty(recordType))
                throw new ArgumentNullException("ViewerId, recordName, and recordType are required.");

            var record = await _context.Records.FirstOrDefaultAsync(r => r.ViewerId == viewerId && r.Name == recordName && r.Type == recordType);
            if (record != null)
            {
                record.ViewerId = null;
                record.AllowViewerAccess = false;
                await _context.SaveChangesAsync();
            }
        }

        public async Task<(User, string)> GeneratePasswordResetTokenAsync(string email)
        {
            if (string.IsNullOrEmpty(email))
                throw new ArgumentNullException(nameof(email));

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null) return (null, null);

            var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
            user.PasswordResetToken = token;
            user.PasswordResetTokenExpiry = DateTime.UtcNow.AddHours(1);

            await _context.SaveChangesAsync();
            return (user, token);
        }

        public async Task<bool> ResetPasswordAsync(string email, string token, string newPassword)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(token) || string.IsNullOrEmpty(newPassword))
                throw new ArgumentNullException("Email, token, and new password are required.");

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null || user.PasswordResetToken != token || user.PasswordResetTokenExpiry <= DateTime.UtcNow)
                return false;

            user.PasswordHash = HashPassword(newPassword);
            user.PasswordResetToken = null;
            user.PasswordResetTokenExpiry = null;

            await _context.SaveChangesAsync();
            return true;
        }

        public string GenerateJwtToken(User user)
        {
            var claims = new List<Claim>
            {
                new Claim("id", user.Id),
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

        internal string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(bytes);
        }

        private bool VerifyPassword(string password, string hash)
        {
            var computedHash = HashPassword(password);
            return computedHash == hash;
        }

        private string GenerateRandomPassword()
        {
            return Guid.NewGuid().ToString("N").Substring(0, 12);
        }
    }
}