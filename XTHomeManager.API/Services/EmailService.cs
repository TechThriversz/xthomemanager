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

        // === PUBLIC METHODS ===
        public async Task SendWelcomeEmailAsync(string toEmail, string name)
        {
            var subject = "Welcome to XT Home Manager!";
            var htmlContent = await LoadTemplateAsync("WelcomeEmailTemplate.html", name);
            await SendEmailAsync(toEmail, subject, htmlContent);
        }

        public async Task SendPasswordResetEmailAsync(string toEmail, string name, string resetLink)
        {
            var subject = "Reset Your Password";
            var htmlContent = await LoadTemplateAsync("ResetPasswordEmailTemplate.html", name, resetLink: resetLink);
            await SendEmailAsync(toEmail, subject, htmlContent);
        }

        public async Task SendInviteEmailAsync(string toEmail, string name, string inviterName, string recordName, string tempPassword)
        {
            var subject = "You've Been Invited to View a Record";
            var htmlContent = await LoadInviteTemplateAsync("InviteEmailTemplate.html", name, inviterName, recordName, tempPassword);
            await SendEmailAsync(toEmail, subject, htmlContent);
        }

        public async Task SendRevokeEmailAsync(string toEmail, string name, string recordName)
        {
            var subject = "Access Revoked";
            var htmlContent = await LoadTemplateAsync("RevokeEmailTemplate.html", name, recordName: recordName);
            await SendEmailAsync(toEmail, subject, htmlContent);
        }

        public async Task SendProUpgradeRequestAsync(string toEmail, string fullName, string phone, string email, DateTime requestDate)
        {
            var subject = $"PRO Upgrade Request - {email}";
            var htmlContent = $@"
        <div style='max-width:600px;margin:auto;font-family:Arial,sans-serif;background:#f9f9f9;padding:30px;border-radius:12px;'>
            <h1 style='color:#1A2A44;text-align:center;'>New PRO Upgrade Request</h1>
            <div style='background:white;padding:20px;border-radius:8px;'>
                <p style='margin:10px 0;'><strong>Full Name:</strong> {fullName ?? "Not provided"}</p>
                <p style='margin:10px 0;'><strong>Phone:</strong> {phone ?? "Not provided"}</p>
                <p style='margin:10px 0;'><strong>Email:</strong> {email}</p>
                <p style='margin:10px 0;'><strong>Requested On:</strong> {requestDate:dddd, MMMM d, yyyy 'at' h:mm tt}</p>
            </div>
            <div style='text-align:center;margin-top:30px;'>
                <a href='https://xthomemanager.vercel.app/admin/users' 
                   style='background:#1A2A44;color:white;padding:14px 32px;text-decoration:none;border-radius:50px;font-weight:bold;'>
                    Open Admin Panel
                </a>
            </div>
            <p style='color:#888;font-size:14px;text-align:center;margin-top:30px;'>© 2025 XT Home Manager</p>
        </div>";

            await SendEmailAsync(toEmail, subject, htmlContent);
        }

        // === PRIVATE HELPERS ===
        private async Task<string> LoadTemplateAsync(string templateName, string name, string resetLink = null, string recordName = null)
        {
            var fullPath = GetFullPath(templateName);
            var html = File.Exists(fullPath) ? await File.ReadAllTextAsync(fullPath) : GetFallbackTemplate(templateName);

            return html
                .Replace("{{FullName}}", name ?? "User")
                .Replace("{{ResetLink}}", resetLink ?? "")
                .Replace("{{record_name}}", recordName ?? "a record");
        }

        private async Task<string> LoadInviteTemplateAsync(string templateName, string name, string inviterName, string recordName, string tempPassword)
        {
            var fullPath = GetFullPath(templateName);
            var html = File.Exists(fullPath) ? await File.ReadAllTextAsync(fullPath) : GetFallbackInviteTemplate();

            html = html
                .Replace("{{Name}}", name ?? "User")
                .Replace("{{InviterName}}", inviterName ?? "Someone")
                .Replace("{{RecordName}}", recordName ?? "a record")
                .Replace("{{TempPassword}}", tempPassword ?? "");

            // Handle conditional
            if (string.IsNullOrEmpty(tempPassword))
            {
                html = html.Replace("{{#if IsNewUser}}", "display:none;").Replace("{{/if}}", "");
            }
            else
            {
                html = html.Replace("{{else}}", "display:none;").Replace("{{/if}}", "");
            }

            return html;
        }

        private string GetFullPath(string templateName)
        {
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "EmailTemplates", templateName);
        }

        private string GetFallbackTemplate(string name) => name switch
        {
            "WelcomeEmailTemplate.html" => GetBeautifulWelcomeFallback(),
            "ResetPasswordEmailTemplate.html" => GetBeautifulResetFallback(),
            "RevokeEmailTemplate.html" => GetBeautifulRevokeFallback(),
            _ => "<h2>Hello</h2><p>Welcome to XT Home Manager.</p>"
        };

        private string GetFallbackInviteTemplate() => @"
            <div style='font-family:Arial,sans-serif;color:#333;padding:20px;'>
                <h2>Hello {{Name}},</h2>
                <p><strong>{{InviterName}}</strong> invited you to view <strong>{{RecordName}}</strong>.</p>
                {{#if IsNewUser}}
                <div style='background:#FFF6F5;padding:15px;border-radius:8px;'>
                    <p><strong>Temp Password:</strong> <code>{{TempPassword}}</code></p>
                </div>
                {{else}}
                <p>Log in to accept.</p>
                {{/if}}
                <a href='https://xthomemanager.vercel.app' style='background:#1A2A44;color:white;padding:12px 24px;text-decoration:none;border-radius:8px;display:inline-block;margin-top:20px;'>
                    Open App
                </a>
            </div>";

        private async Task SendEmailAsync(string toEmail, string subject, string htmlContent)
        {
            var settings = _configuration.GetSection("EmailSettings");
            var message = new MimeMessage();
            message.From.Add(MailboxAddress.Parse(settings["SenderEmail"]));
            message.To.Add(MailboxAddress.Parse(toEmail));
            message.Subject = subject;
            message.Body = new TextPart("html") { Text = htmlContent };

            using var client = new SmtpClient();
            await client.ConnectAsync(settings["SmtpServer"], int.Parse(settings["Port"]), SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(settings["SenderEmail"], settings["Password"]);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);
        }

        // === FALLBACK TEMPLATES (BEAUTIFUL) ===
        private string GetBeautifulWelcomeFallback() => @"
            <div style='max-width:600px;margin:auto;font-family:Arial,sans-serif;background:#f9f9f9;padding:30px;border-radius:12px;'>
                <h1 style='color:#1A2A44;text-align:center;'>Welcome to XT Home Manager!</h1>
                <p style='font-size:16px;color:#555;'>Hello <strong>{{FullName}}</strong>,</p>
                <p style='font-size:16px;color:#555;line-height:1.6;'>Your account is ready. Start tracking milk, rent, bills, and more — all in one beautiful place.</p>
                <div style='text-align:center;margin:30px 0;'>
                    <a href='https://xthomemanager.vercel.app' style='background:#1A2A44;color:white;padding:14px 32px;text-decoration:none;border-radius:50px;font-weight:bold;'>
                        Open Dashboard
                    </a>
                </div>
                <p style='color:#888;font-size:14px;text-align:center;'>© 2025 XT Home Manager</p>
            </div>";

        private string GetBeautifulResetFallback() => @"
            <div style='max-width:600px;margin:auto;font-family:Arial,sans-serif;background:#f9f9f9;padding:30px;border-radius:12px;'>
                <h1 style='color:#1A2A44;text-align:center;'>Reset Your Password</h1>
                <p style='font-size:16px;color:#555;'>Hello <strong>{{FullName}}</strong>,</p>
                <p style='font-size:16px;color:#555;line-height:1.6;'>Click below to reset your password. Link expires in 1 hour.</p>
                <div style='text-align:center;margin:30px 0;'>
                    <a href='{{ResetLink}}' style='background:#1A2A44;color:white;padding:14px 32px;text-decoration:none;border-radius:50px;font-weight:bold;'>
                        Reset Password
                    </a>
                </div>
                <p style='color:#888;font-size:14px;text-align:center;'>Ignore if you didn't request this.</p>
            </div>";

        private string GetBeautifulRevokeFallback() => @"
            <div style='max-width:600px;margin:auto;font-family:Arial,sans-serif;background:#f9f9f9;padding:30px;border-radius:12px;'>
                <h1 style='color:#1A2A44;text-align:center;'>Access Revoked</h1>
                <p style='font-size:16px;color:#555;'>Hello <strong>{{FullName}}</strong>,</p>
                <p style='font-size:16px;color:#555;line-height:1.6;'>Your access to <strong>{{record_name}}</strong> has been revoked by the owner.</p>
                <p style='color:#888;font-size:14px;text-align:center;'>© 2025 XT Home Manager</p>
            </div>";
    }
}