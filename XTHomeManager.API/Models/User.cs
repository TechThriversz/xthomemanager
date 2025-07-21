namespace XTHomeManager.API.Models
{
    public class User
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Email { get; set; }
        public string FullName { get; set; } // For display
        public string PasswordHash { get; set; }
        public string Role { get; set; }
        public string? AdminId { get; set; }
    }
}
}