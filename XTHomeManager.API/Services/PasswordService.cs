// Services/PasswordService.cs
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using XTHomeManager.API.Data;
using XTHomeManager.API.DTOs;
using XTHomeManager.API.Models;

namespace XTHomeManager.API.Services
{
    public class PasswordService
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _config;

        public PasswordService(AppDbContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }

        private string Encrypt(string plainText)
        {
            var key = _config["Encryption:Key"] ?? "your-32-char-key-here-1234567890";
            byte[] iv = new byte[16];
            byte[] array;

            using (Aes aes = Aes.Create())
            {
                aes.Key = Encoding.UTF8.GetBytes(key.PadRight(32).Substring(0, 32));
                aes.GenerateIV();
                iv = aes.IV;

                ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
                using MemoryStream ms = new();
                ms.Write(iv, 0, iv.Length);
                using (CryptoStream cs = new(ms, encryptor, CryptoStreamMode.Write))
                using (StreamWriter sw = new(cs))
                    sw.Write(plainText);
                array = ms.ToArray();
            }

            return Convert.ToBase64String(array);
        }

        private string Decrypt(string cipherText)
        {
            var key = _config["Encryption:Key"] ?? "your-32-char-key-here-1234567890";
            byte[] buffer = Convert.FromBase64String(cipherText);
            using Aes aes = Aes.Create();
            aes.Key = Encoding.UTF8.GetBytes(key.PadRight(32).Substring(0, 32));
            byte[] iv = new byte[16];
            Array.Copy(buffer, 0, iv, 0, iv.Length);
            aes.IV = iv;

            ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
            using MemoryStream ms = new(buffer, 16, buffer.Length - 16);
            using CryptoStream cs = new(ms, decryptor, CryptoStreamMode.Read);
            using StreamReader sr = new(cs);
            return sr.ReadToEnd();
        }

        public async Task<Password?> AddPasswordAsync(string userId, PasswordDto dto)
        {
         

            var password = new Password
            {
                UserId = userId,
                AccountName = dto.AccountName,
                Email = dto.Email,
                Username = dto.Username,
                EncryptedPassword = !string.IsNullOrEmpty(dto.Password) ? Encrypt(dto.Password) : null,
                SecurityMethod = dto.SecurityMethod,
                SecurityValue = dto.SecurityValue,
                AssociatedPhone = dto.AssociatedPhone,
                RecoveryEmail = dto.RecoveryEmail,
                SecurityQuestions = dto.SecurityQuestions?.Any() == true
                    ? JsonSerializer.Serialize(dto.SecurityQuestions) : null
            };

            _context.Passwords.Add(password);
            await _context.SaveChangesAsync();
            return password;
        }

        public async Task<Password?> UpdatePasswordAsync(int id, string userId, PasswordDto dto)
        {
            var password = await _context.Passwords.FirstOrDefaultAsync(p => p.Id == id && p.UserId == userId);
            if (password == null) return null;

            password.AccountName = dto.AccountName;
            password.Email = dto.Email;
            password.Username = dto.Username;
            password.EncryptedPassword = !string.IsNullOrEmpty(dto.Password) ? Encrypt(dto.Password) : password.EncryptedPassword;
            password.SecurityMethod = dto.SecurityMethod;
            password.SecurityValue = dto.SecurityValue;
            password.AssociatedPhone = dto.AssociatedPhone;
            password.RecoveryEmail = dto.RecoveryEmail;
            password.SecurityQuestions = dto.SecurityQuestions?.Any() == true
                ? JsonSerializer.Serialize(dto.SecurityQuestions) : password.SecurityQuestions;
            password.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return password;
        }

        public async Task<bool> DeletePasswordAsync(int id, string userId)
        {
            var password = await _context.Passwords.FirstOrDefaultAsync(p => p.Id == id && p.UserId == userId);
            if (password == null) return false;

            _context.Passwords.Remove(password);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<Password>> GetPasswordsAsync(string userId)
        {
            return await _context.Passwords
                .Where(p => p.UserId == userId)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }

        public string? DecryptPassword(string? encrypted)
        {
            return string.IsNullOrEmpty(encrypted) ? null : Decrypt(encrypted);
        }

        public async Task<Password?> GetPasswordByIdAsync(int id, string userId)
        {
            return await _context.Passwords
                .FirstOrDefaultAsync(p => p.Id == id && p.UserId == userId);
        }
    }
}