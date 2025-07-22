using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace XTHomeManager.API.Models
{
    public class Record
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        [Required]
        public string Type { get; set; }

        [Required]
        public string UserId { get; set; }

        public string? ViewerId { get; set; }

        public bool AllowViewerAccess { get; set; }

        [JsonIgnore] // Exclude User from JSON serialization
        public User? User { get; set; }
    }
}