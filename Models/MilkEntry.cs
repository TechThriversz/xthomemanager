namespace XTHomeManager.API.Models
{
    public class MilkEntry
    {
        public int Id { get; set; }
        public DateTime Date { get; set; }
        public decimal QuantityLiters { get; set; }
        public decimal RatePerLiter { get; set; }
        public string Status { get; set; } // "Bought" or "Leave"
        public decimal TotalCost => QuantityLiters * RatePerLiter;
        public string AdminId { get; set; } // Links to Admin user
        public bool AllowViewerAccess { get; set; } // Permission for Viewers
    }
}