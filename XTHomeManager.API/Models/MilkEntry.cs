namespace XTHomeManager.API.Models
{
    public class MilkEntry
    {
        public int Id { get; set; }
        public int RecordId { get; set; } // Links to Record
        public DateTime Date { get; set; }
        public decimal QuantityLiters { get; set; }
        public string Status { get; set; } // "Bought" or "Leave"
        public decimal RatePerLiter { get; set; } // From Settings
        public decimal TotalCost => Status == "Bought" ? QuantityLiters * RatePerLiter : 0; // Calculated
        public string AdminId { get; set; } // Links to Admin user
    }
}