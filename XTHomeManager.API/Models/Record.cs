namespace XTHomeManager.API.Models
{
    public class Record
    {
        public int Id { get; set; }
        public string Name { get; set; } // e.g., "Lahore Milk"
        public string Type { get; set; } // "Milk", "Bill", "Rent"
        public string UserId { get; set; } // Admin who created
        public string? ViewerId { get; set; } // Viewer with access
        public bool AllowViewerAccess { get; set; } // Permission for Viewers
        public User User { get; set; } // Navigation property for FullName
    }
}