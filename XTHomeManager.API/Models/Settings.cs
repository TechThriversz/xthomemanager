namespace XTHomeManager.API.Models
{
    public class Settings
    {
        public int Id { get; set; }
        public string UserId { get; set; } // Admin who set the rate
        public decimal MilkRatePerLiter { get; set; } // Rate for milk calculations
    }
}