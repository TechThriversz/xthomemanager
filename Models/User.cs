namespace XTHomeManager.API.Models
{
    public class User
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Email { get; set; }
        public string PasswordHash { get; set; }
        public string Role { get; set; } // "Admin" or "Viewer"
        public string? AdminId { get; set; } // For Viewers, links to Admin
    }
}