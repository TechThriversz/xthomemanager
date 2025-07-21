namespace XTHomeManager.API.Models
{
    public class ElectricityBill
    {
        public int Id { get; set; }
        public int RecordId { get; set; } // Links to Record
        public string Month { get; set; } // e.g., "2025-07"
        public decimal Amount { get; set; }
        public string ReferenceNumber { get; set; }
        public string? FilePath { get; set; } // Optional file path
        public string AdminId { get; set; } // Links to Admin user
    }
}