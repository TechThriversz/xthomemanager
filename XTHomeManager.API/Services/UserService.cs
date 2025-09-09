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

        public async Task<User> GetUserByIdAsync(string id)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.Id == id);
        }

        public async Task<User> GetUserByEmailAsync(string email)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
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

        // UserService.cs (Update InviteOrUpdateViewerAsync)
        public async Task<(User, string)> InviteOrUpdateViewerAsync(string email, string inviterName, string adminId, string recordName, int? recordId = null)
        {
            var adminRecord = recordId.HasValue
                ? await _context.Records.FirstOrDefaultAsync(r => r.Id == recordId.Value && r.UserId == adminId)
                : await _context.Records.FirstOrDefaultAsync(r => r.Name == recordName && r.UserId == adminId);

            if (adminRecord == null) return (null, "Record not found for this admin.");

            var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (existingUser != null)
            {
                var existingViewer = await _context.RecordViewers
                    .FirstOrDefaultAsync(rv => rv.RecordId == adminRecord.Id && rv.UserId == existingUser.Id);
                if (existingViewer != null)
                {
                    return (null, $"User already viewer in this record: {adminRecord.Name}");
                }
                _context.RecordViewers.Add(new RecordViewer { RecordId = adminRecord.Id, UserId = existingUser.Id, AllowViewerAccess = true, IsAccepted = false });
                await _context.SaveChangesAsync();
                return (existingUser, $"User {email} added as viewer for {adminRecord.Name}. Notification sent.");
            }

            var tempPassword = GenerateRandomPassword();
            var newUser = new User
            {
                Email = email,
                FullName = email.Split('@')[0],
                PasswordHash = HashPassword(tempPassword),
                Role = "Viewer",
                AdminId = adminId,
                PasswordResetTokenExpiry = DateTime.UtcNow.AddHours(24)
            };
            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();

            _context.RecordViewers.Add(new RecordViewer { RecordId = adminRecord.Id, UserId = newUser.Id, AllowViewerAccess = true, IsAccepted = false });
            await _context.SaveChangesAsync();

            return (newUser, $"New user {email} invited as viewer for {adminRecord.Name} with temporary password: {tempPassword}");
        }

        public async Task<List<InvitedViewerDto>> GetInvitedViewersAsync(string adminId)
        {
            return await _context.Users
                .Where(u => u.AdminId == adminId && u.Role == "Viewer")
                .Select(u => new InvitedViewerDto
                {
                    Id = u.Id,
                    Email = u.Email,
                    FullName = u.FullName,
                    Role = u.Role,
                    Records = _context.RecordViewers
                        .Where(rv => rv.UserId == u.Id && rv.Record.UserId == adminId)
                        .Select(rv => new RecordDto
                        {
                            Id = rv.Record.Id,
                            Name = rv.Record.Name,
                            Type = rv.Record.Type,
                            Accepted = rv.AllowViewerAccess,
                            IsAccepted = rv.IsAccepted
                        })
                        .ToList()
                })
                .ToListAsync();
        }

        public async Task RevokeViewerAccessAsync(string viewerId, int recordId)
        {
            if (string.IsNullOrEmpty(viewerId) || recordId <= 0)
                throw new ArgumentNullException("ViewerId and recordId are required.");

            var viewerRecord = await _context.RecordViewers
                .FirstOrDefaultAsync(rv => rv.UserId == viewerId && rv.RecordId == recordId);

            if (viewerRecord == null)
                throw new Exception("Viewer access not found for the specified record.");

            // Set AllowViewerAccess to false instead of deleting
            viewerRecord.AllowViewerAccess = false;
            await _context.SaveChangesAsync();
        }
       

        public async Task<(User, string)> GeneratePasswordResetTokenAsync(string email)
        {
            if (string.IsNullOrEmpty(email))
                throw new ArgumentNullException(nameof(email));

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null) return (null, null);

            var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
            user.PasswordResetToken = token;
            user.PasswordResetTokenExpiry = DateTime.UtcNow.AddHours(24);

            await _context.SaveChangesAsync();
            return (user, token);
        }

        // UserService.cs
        public async Task<bool> ResetPasswordAsync(string email, string token, string newPassword)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(token) || string.IsNullOrEmpty(newPassword))
                throw new ArgumentNullException("Email, token, and new password are required.");

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
            {
                Console.WriteLine($"User not found for email: {email}");
                return false;
            }

            if (user.PasswordResetToken != token)
            {
                Console.WriteLine($"Token mismatch for email: {email}. Expected: {user.PasswordResetToken}, Received: {token}");
                return false;
            }

            if (user.PasswordResetTokenExpiry <= DateTime.UtcNow)
            {
                Console.WriteLine($"Token expired for email: {email}. Expiry: {user.PasswordResetTokenExpiry}, Now: {DateTime.UtcNow}");
                return false;
            }

            user.PasswordHash = HashPassword(newPassword);
            user.PasswordResetToken = null;
            user.PasswordResetTokenExpiry = null;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task ClearTemporaryPasswordAsync(string userId)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user != null)
            {
                user.PasswordResetTokenExpiry = null;
                await _context.SaveChangesAsync();
            }
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
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

        public string GetTemporaryPassword()
        {
            return GenerateRandomPassword();
        }

        private string GenerateRandomPassword()
        {
            return Guid.NewGuid().ToString("N").Substring(0, 12);
        }
    }
}