namespace XTHomeManager.API.Models
{
    public class UserDeletionRequest
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public DateTime RequestDate { get; set; } = DateTime.UtcNow;
        public string Status { get; set; } = "Pending"; // Pending, Approved, Rejected
        public DateTime? ApprovedDate { get; set; }
        public DateTime? DeletionScheduledAt { get; set; }
        public User User { get; set; } = null!;
    }
}
