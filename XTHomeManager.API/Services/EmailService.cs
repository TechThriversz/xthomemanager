using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using MimeKit;
using System.IO;
using System.Threading.Tasks;

namespace XTHomeManager.API.Services
{
    public class EmailService
    {
        private readonly IConfiguration _configuration;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        public async Task SendWelcomeEmailAsync(string toEmail, string name)
        {
            var subject = "Welcome to XT Home Manager!";
            var templatePath = Path.Combine("EmailTemplates", "WelcomeEmailTemplate.html");
            var htmlContent = await LoadTemplateAsync(templatePath, name);
            await SendEmailAsync(toEmail, subject, htmlContent);
        }

        public async Task SendPasswordResetEmailAsync(string toEmail, string name, string resetLink)
        {
            var subject = "Your Password Reset Request";
            var templatePath = Path.Combine("EmailTemplates", "ResetPasswordEmailTemplate.html");
            var htmlContent = await LoadTemplateAsync(templatePath, name, resetLink);
            await SendEmailAsync(toEmail, subject, htmlContent);
        }

        private async Task<string> LoadTemplateAsync(string templatePath, string name, string resetLink = null)
        {
            if (!File.Exists(templatePath))
                throw new FileNotFoundException($"Email template not found at {templatePath}");

            var htmlContent = await File.ReadAllTextAsync(templatePath);
            htmlContent = htmlContent.Replace("{{name}}", name ?? "User");
            if (resetLink != null) htmlContent = htmlContent.Replace("{{reset_link}}", resetLink);
            return htmlContent;
        }

        private async Task SendEmailAsync(string toEmail, string subject, string htmlContent)
        {
            var emailSettings = _configuration.GetSection("EmailSettings");
            if (emailSettings == null) throw new InvalidOperationException("Email settings not configured.");

            var email = new MimeMessage();
            email.From.Add(new MailboxAddress(emailSettings["SenderName"], emailSettings["SenderEmail"]));
            email.To.Add(MailboxAddress.Parse(toEmail));
            email.Subject = subject;

            var builder = new BodyBuilder { HtmlBody = htmlContent };
            email.Body = builder.ToMessageBody();

            using var smtp = new SmtpClient();
            await smtp.ConnectAsync(emailSettings["SmtpServer"], int.Parse(emailSettings["Port"]), SecureSocketOptions.StartTls);
            await smtp.AuthenticateAsync(emailSettings["SenderEmail"], emailSettings["Password"]);
            await smtp.SendAsync(email);
            await smtp.DisconnectAsync(true);
        }
    }
}