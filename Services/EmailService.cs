using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace AdminMembers.Services
{
    public class EmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<bool> SendPasswordResetAsync(string toEmail, string toName, string resetToken, string appBaseUrl)
        {
            var resetUrl = $"{appBaseUrl}/ResetPassword?token={Uri.EscapeDataString(resetToken)}&email={Uri.EscapeDataString(toEmail)}";

            var subject = "Password Reset Request";
            var html = $"""
                <div style="font-family:sans-serif;max-width:480px;margin:auto;padding:32px;border:1px solid #e0e0e0;border-radius:8px;">
                    <h2 style="color:#333;">Password Reset</h2>
                    <p>Hello <strong>{toName}</strong>,</p>
                    <p>We received a request to reset your password. Click the button below to choose a new one.</p>
                    <a href="{resetUrl}"
                       style="display:inline-block;margin:20px 0;padding:12px 24px;background:#4f46e5;color:#fff;
                              text-decoration:none;border-radius:6px;font-weight:600;">
                        Reset Password
                    </a>
                    <p style="font-size:12px;color:#888;">This link expires in 1 hour. If you did not request a password reset, you can safely ignore this email.</p>
                </div>
                """;

            return await SendEmailAsync(toEmail, toName, subject, html);
        }

        public async Task<bool> SendEmailAsync(string toEmail, string toName, string subject, string htmlBody)
        {
            var host    = _configuration["Email:SmtpHost"];
            var portStr = _configuration["Email:SmtpPort"];
            var useSsl  = _configuration.GetValue<bool>("Email:UseSsl");
            var user    = _configuration["Email:Username"];
            var pass    = _configuration["Email:Password"];
            var from    = _configuration["Email:FromAddress"];
            var fromName = _configuration["Email:FromName"] ?? "Gilde Van De Stroppendragers";

            if (string.IsNullOrEmpty(host) || string.IsNullOrEmpty(from))
            {
                _logger.LogWarning("Email is not configured. Skipping send to {ToEmail}.", toEmail);
                return false;
            }

            if (!int.TryParse(portStr, out var port)) port = 587;

            try
            {
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(fromName, from));
                message.To.Add(new MailboxAddress(toName, toEmail));
                message.Subject = subject;
                message.Body = new TextPart("html") { Text = htmlBody };

                using var client = new SmtpClient();
                var secureSocket = useSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.Auto;
                await client.ConnectAsync(host, port, secureSocket);

                if (!string.IsNullOrEmpty(user) && !string.IsNullOrEmpty(pass))
                    await client.AuthenticateAsync(user, pass);

                await client.SendAsync(message);
                await client.DisconnectAsync(true);

                _logger.LogInformation("Email sent to {ToEmail}: {Subject}", toEmail, subject);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {ToEmail}", toEmail);
                return false;
            }
        }
    }
}
