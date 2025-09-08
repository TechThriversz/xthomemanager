// Models.cs
namespace XTHomeManager.API.Models
{
    public class LoginModel
    {
        public string Email { get; set; }
        public string Password { get; set; }
    }

    public class RegisterModel
    {
        public string Email { get; set; }
        public string FullName { get; set; }
        public string Password { get; set; }
    }

    public class ForgotPasswordModel
    {
        public string Email { get; set; }
    }

    public class ResetPasswordModel
    {
        public string Email { get; set; }
        public string Token { get; set; }
        public string NewPassword { get; set; }
    }

    public class InviteModel
    {
        public string Email { get; set; }
        public string RecordName { get; set; } // Keep for backward compatibility
        public int RecordId { get; set; } // New field for specific record linking
    }

    public class RevokeModel
    {
        public string ViewerId { get; set; }
        public int RecordId { get; set; }
    }
}