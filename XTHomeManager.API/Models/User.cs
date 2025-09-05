using System.Text.Json.Serialization;

namespace XTHomeManager.API.Models
{
    public class User
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Email { get; set; }
        public string FullName { get; set; }
        public string PasswordHash { get; set; }
        public string Role { get; set; }
        public string? AdminId { get; set; }
        public string? ImagePath { get; set; }
        public string? PasswordResetToken { get; set; } // Added for password reset token
        public DateTime? PasswordResetTokenExpiry { get; set; } // Added for token expirations

        [JsonIgnore]
        public ICollection<RecordViewer> ViewerRecords { get; set; }
    }
}