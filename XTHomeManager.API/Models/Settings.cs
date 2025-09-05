using System.ComponentModel.DataAnnotations;

namespace XTHomeManager.API.Models
{
    public class Settings
    {
        public int Id { get; set; } // Keep id, but it's not used for updates
        [Required] // Required for creation, but optional for updates
        public string UserId { get; set; }
        public decimal MilkRatePerLiter { get; set; }
    }
}