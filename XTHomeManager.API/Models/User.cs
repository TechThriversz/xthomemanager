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
        public string? PasswordResetToken { get; set; } 
        public DateTime? PasswordResetTokenExpiry { get; set; } 
        public string? PhoneNumber { get; set; }
        public string? Gender { get; set; }
        public DateTime? DateOfBirth { get; set; }

        public bool IsActive { get; set; } = true;

        // PRO FIELDS
        public bool IsPro { get; set; } = false;
        public DateTime? ProStartDate { get; set; }
        public DateTime? ProEndDate { get; set; }

        //NEW PERMISSION SYSTEM
        public bool CanUsePasswordVault { get; set; } = false;
        public bool CanUseFamilyMembers { get; set; } = false;
        public bool CanUseMedicalRecords { get; set; } = false;

        [JsonIgnore]
        public ICollection<RecordViewer> ViewerRecords { get; set; }

        // Navigation
        public ICollection<Password> Passwords { get; set; } = new List<Password>();

        public ICollection<ProUpgradeRequest> ProUpgradeRequests { get; set; } = new List<ProUpgradeRequest>();
    }
}