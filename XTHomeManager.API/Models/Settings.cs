
using System.ComponentModel.DataAnnotations;

namespace XTHomeManager.API.Models
{
    public class Settings
    {
        public int Id { get; set; }
        [Required]
        public string UserId { get; set; } = string.Empty;

        // NEW SETTINGS
        public string Currency { get; set; } = "PKR"; // Default: PKR
        public string Country { get; set; } = "Pakistan"; // Default
        public int DecimalPlaces { get; set; } = 0;
        public string DateFormat { get; set; } = "dd/MM/yyyy"; // en-GB
        public string WeightUnit { get; set; } = "kg"; // kg or lbs

        // Existing
        public decimal MilkRatePerLiter { get; set; } = 0;
    }
}