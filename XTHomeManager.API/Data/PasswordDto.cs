// DTOs/PasswordDto.cs
namespace XTHomeManager.API.DTOs
{
    public class PasswordDto
    {
        public string? AccountName { get; set; }
        public string Url { get; set; }
        public string Category { get; set; }
        public string? Email { get; set; }
        public string? Username { get; set; }
        public string? Password { get; set; }
        public string? SecurityMethod { get; set; }
        public string? SecurityValue { get; set; }
        public string? AssociatedPhone { get; set; }
        public string? RecoveryEmail { get; set; }
        public List<SecurityQuestion>? SecurityQuestions { get; set; }
    }

    public class SecurityQuestion
    {
        public string Question { get; set; } = string.Empty;
        public string Answer { get; set; } = string.Empty;
    }
}