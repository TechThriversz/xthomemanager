using System.ComponentModel.DataAnnotations;

namespace XTHomeManager.API.Models
{
    public class ProUpgradeRequest
    {
        public int Id { get; set; }
        public string UserId { get; set; } = null!;
        public User User { get; set; } = null!;
        public DateTime RequestDate { get; set; }
        public DateTime? ApprovedDate { get; set; }
        public string Status { get; set; } = "Pending"; // Pending, Approved, Rejected
    }
}
