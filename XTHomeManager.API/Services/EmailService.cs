// EmailService.cs
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
            var htmlContent = await LoadTemplateAsync(templatePath, name, resetLink: resetLink);
            await SendEmailAsync(toEmail, subject, htmlContent);
        }

        public async Task SendInviteEmailAsync(string toEmail, string name, string inviterName, string recordName, string tempPassword)
        {
            var subject = "You’ve Been Invited to View a Record on XT Home Manager";
            var templatePath = Path.Combine("EmailTemplates", "InviteEmailTemplate.html");

            var htmlContent = await LoadTemplateInviteAsync(
                templatePath,
                name,
                inviterName,
                recordName,
                tempPassword
            );

            await SendEmailAsync(toEmail, subject, htmlContent);
        }

        private async Task<string> LoadTemplateInviteAsync(
    string templatePath,
    string name,
    string inviterName = null,
    string recordName = null,
    string tempPassword = null)
        {
            // Fix path for production
            var basePath = AppDomain.CurrentDomain.BaseDirectory;
            var fullPath = Path.Combine(basePath, templatePath);

            if (!File.Exists(fullPath))
            {
                // Fallback: inline HTML (prevents 500)
                return $@"
            <h2>Hello {name},</h2>
            <p><strong>{inviterName}</strong> invited you to view <strong>{recordName}</strong>.</p>
            {(tempPassword != null ? $"<p><strong>Temp Password:</strong> <code>{tempPassword}</code></p>" : "<p>Log in to accept.</p>")}
            <a href='https://xthomemanager.vercel.app'>Open App</a>
        ";
            }

            var html = await File.ReadAllTextAsync(fullPath);

            // Replace known placeholders
            html = html
                .Replace("{{Name}}", name ?? "User")
                .Replace("{{InviterName}}", inviterName ?? "Someone")
                .Replace("{{RecordName}}", recordName ?? "a record")
                .Replace("{{TempPassword}}", tempPassword ?? "");

            // Handle conditional block
            if (tempPassword != null)
            {
                html = html.Replace("{{#if IsNewUser}}", "").Replace("{{/if}}", "");
                html = html.Replace("{{else}}", ""); // Remove else part
            }
            else
            {
                // Remove entire {{#if}} block
                var ifBlock = html.Split(new[] { "{{#if IsNewUser}}" }, StringSplitOptions.None)[1]
                                  .Split(new[] { "{{/if}}" }, StringSplitOptions.None)[0];
                var elseBlock = ifBlock.Split(new[] { "{{else}}" }, StringSplitOptions.None);
                var newUserBlock = elseBlock[0];
                var existingUserBlock = elseBlock.Length > 1 ? elseBlock[1] : "";

                html = html.Replace("{{#if IsNewUser}}" + ifBlock + "{{/if}}", existingUserBlock);
            }

            return html;
        }

        public async Task SendRevokeEmailAsync(string toEmail, string name, string recordName)
        {
            var subject = "Your Access to a Record Has Been Revoked";
            var templatePath = Path.Combine("EmailTemplates", "RevokeEmailTemplate.html");
            var htmlContent = await LoadTemplateAsync(templatePath, name, recordName: recordName); // Changed recordId to recordName
            await SendEmailAsync(toEmail, subject, htmlContent);
        }
        private async Task<string> LoadTemplateAsync(string templatePath, string name, string inviterName = null, string recordName = null, string tempPassword = null, string resetLink = null, string recordId = null)
        {
            if (!File.Exists(templatePath))
                throw new FileNotFoundException($"Email template not found at {templatePath}");

            var htmlContent = await File.ReadAllTextAsync(templatePath);
            htmlContent = htmlContent.Replace("{{FullName}}", name ?? "User");
            if (resetLink != null) htmlContent = htmlContent.Replace("{{ResetLink}}", resetLink);
            if (inviterName != null) htmlContent = htmlContent.Replace("{{inviter_name}}", inviterName);
            if (recordName != null) htmlContent = htmlContent.Replace("{{record_name}}", recordName);
            if (tempPassword != null) htmlContent = htmlContent.Replace("{{temp_password}}", tempPassword);
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