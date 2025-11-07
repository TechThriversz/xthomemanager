// Models/Password.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace XTHomeManager.API.Models
{
    public class Password
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        public string? AccountName { get; set; }
        public string? Email { get; set; }
        public string? Username { get; set; }

        // Encrypted in DB
        public string? EncryptedPassword { get; set; }

        public string? SecurityMethod { get; set; } // Phone, Email, AuthApp
        public string? SecurityValue { get; set; }  // +923001234567, recovery@gmail.com, etc.

        public string? AssociatedPhone { get; set; }
        public string? RecoveryEmail { get; set; }

        // JSON stored
        public string? SecurityQuestions { get; set; } // e.g. [{"q":"Pet name?","a":"Max"}]

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        [ForeignKey("UserId")]
        public User User { get; set; } = null!;
    }
}