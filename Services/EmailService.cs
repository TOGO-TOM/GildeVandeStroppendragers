using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using AdminMembers.Data;
using AdminMembers.Models;
using Microsoft.EntityFrameworkCore;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace AdminMembers.Services
{
    public class EmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;
        private readonly ApplicationDbContext _context;
        private readonly IHttpClientFactory _httpClientFactory;

        public EmailService(ApplicationDbContext context, IConfiguration configuration, ILogger<EmailService> logger, IHttpClientFactory httpClientFactory)
        {
            _context = context;
            _configuration = configuration;
            _logger = logger;
            _httpClientFactory = httpClientFactory;
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
            // Try to load settings from the database first
            var dbSettings = await _context.EmailSettings.FirstOrDefaultAsync();

            if (dbSettings != null && !string.IsNullOrEmpty(dbSettings.FromAddress))
            {
                var provider = dbSettings.Provider ?? "Smtp";

                if (string.Equals(provider, "MailerSend", StringComparison.OrdinalIgnoreCase))
                {
                    return await SendViaMailerSendAsync(dbSettings, toEmail, toName, subject, htmlBody);
                }

                return await SendViaSmtpAsync(dbSettings, toEmail, toName, subject, htmlBody);
            }

            // Fallback to appsettings.json configuration (SMTP only)
            return await SendViaSmtpFallbackAsync(toEmail, toName, subject, htmlBody);
        }

        private async Task<bool> SendViaMailerSendAsync(EmailSettings settings, string toEmail, string toName, string subject, string htmlBody)
        {
            if (string.IsNullOrEmpty(settings.ApiKey))
            {
                _logger.LogWarning("MailerSend API key is not configured. Skipping send to {ToEmail}.", toEmail);
                return false;
            }

            try
            {
                var payload = new
                {
                    from = new
                    {
                        email = settings.FromAddress,
                        name = settings.FromName ?? "Gilde Van De Stroppendragers"
                    },
                    to = new[]
                    {
                        new { email = toEmail, name = toName }
                    },
                    subject,
                    html = htmlBody
                };

                var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
                using var client = _httpClientFactory.CreateClient();
                using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.mailersend.com/v1/email");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", settings.ApiKey);
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await client.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    var messageId = response.Headers.TryGetValues("x-message-id", out var values)
                        ? values.FirstOrDefault() : null;
                    _logger.LogInformation("MailerSend email queued to {ToEmail}: {Subject} (x-message-id: {MessageId})", toEmail, subject, messageId);
                    return true;
                }

                var body = await response.Content.ReadAsStringAsync();
                _logger.LogError("MailerSend API returned {StatusCode}: {Body}", (int)response.StatusCode, body);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email via MailerSend to {ToEmail}", toEmail);
                return false;
            }
        }

        private async Task<bool> SendViaSmtpAsync(EmailSettings settings, string toEmail, string toName, string subject, string htmlBody)
        {
            var host     = settings.SmtpHost;
            var port     = settings.SmtpPort;
            var useSsl   = settings.UseSsl;
            var from     = settings.FromAddress;
            var fromName = settings.FromName ?? "Gilde Van De Stroppendragers";

            string? user, pass;

            if (string.Equals(settings.Provider, "ApiKey", StringComparison.OrdinalIgnoreCase)
                && !string.IsNullOrEmpty(settings.ApiKey))
            {
                // SendGrid / Mailgun style: authenticate via SMTP with "apikey" as username
                user = "apikey";
                pass = settings.ApiKey;
            }
            else
            {
                user = settings.Username;
                pass = settings.Password;
            }

            if (string.IsNullOrEmpty(host) || string.IsNullOrEmpty(from))
            {
                _logger.LogWarning("SMTP email is not configured. Skipping send to {ToEmail}.", toEmail);
                return false;
            }

            return await SendSmtpMessageAsync(host, port, useSsl, user, pass, from, fromName, toEmail, toName, subject, htmlBody);
        }

        private async Task<bool> SendViaSmtpFallbackAsync(string toEmail, string toName, string subject, string htmlBody)
        {
            var host     = _configuration["Email:SmtpHost"];
            var portStr  = _configuration["Email:SmtpPort"];
            var useSsl   = _configuration.GetValue<bool>("Email:UseSsl");
            var user     = _configuration["Email:Username"];
            var pass     = _configuration["Email:Password"];
            var from     = _configuration["Email:FromAddress"];
            var fromName = _configuration["Email:FromName"] ?? "Gilde Van De Stroppendragers";

            if (!int.TryParse(portStr, out var port)) port = 587;

            if (string.IsNullOrEmpty(host) || string.IsNullOrEmpty(from))
            {
                _logger.LogWarning("Email is not configured. Skipping send to {ToEmail}.", toEmail);
                return false;
            }

            return await SendSmtpMessageAsync(host, port, useSsl, user, pass, from, fromName, toEmail, toName, subject, htmlBody);
        }

        private async Task<bool> SendSmtpMessageAsync(string host, int port, bool useSsl,
            string? user, string? pass, string from, string fromName,
            string toEmail, string toName, string subject, string htmlBody)
        {
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
