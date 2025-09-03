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
        public string RecordName { get; set; }
    }

    public class RevokeModel
    {
        public string ViewerId { get; set; }
        public string RecordName { get; set; }
        public string RecordType { get; set; }
    }
}