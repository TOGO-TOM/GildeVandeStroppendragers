using OtpNet;
using QRCoder;
using AdminMembers.Data;
using AdminMembers.Models;
using Microsoft.EntityFrameworkCore;

namespace AdminMembers.Services
{
    public class TotpService
    {
        private readonly ApplicationDbContext _context;
        private const string Issuer = "GildeVanDeStroppendragers";
        private const string TrustedDeviceCookie = "TrustedDevice";
        private const int TrustDays = 30;

        public TotpService(ApplicationDbContext context) => _context = context;

        // Generate a new Base32 secret key
        public string GenerateSecret()
        {
            var key = KeyGeneration.GenerateRandomKey(20);
            return Base32Encoding.ToString(key);
        }

        // Build the otpauth:// URI for QR scanning
        public string GetOtpUri(string secret, string username)
            => $"otpauth://totp/{Uri.EscapeDataString(Issuer)}:{Uri.EscapeDataString(username)}" +
               $"?secret={secret}&issuer={Uri.EscapeDataString(Issuer)}&algorithm=SHA1&digits=6&period=30";

        // Return QR code as a Base64-encoded PNG data URI
        public string GetQrCodeDataUri(string otpUri)
        {
            using var qrGenerator = new QRCodeGenerator();
            var qrData = qrGenerator.CreateQrCode(otpUri, QRCodeGenerator.ECCLevel.Q);
            using var qrCode = new PngByteQRCode(qrData);
            var png = qrCode.GetGraphic(6);
            return "data:image/png;base64," + Convert.ToBase64String(png);
        }

        // Verify a 6-digit TOTP code against a secret
        public bool VerifyCode(string secret, string code)
        {
            if (string.IsNullOrWhiteSpace(secret) || string.IsNullOrWhiteSpace(code))
                return false;

            try
            {
                var keyBytes = Base32Encoding.ToBytes(secret);
                var totp = new Totp(keyBytes);
                // Allow ±1 window (30 sec tolerance)
                return totp.VerifyTotp(code.Trim(), out _, new VerificationWindow(1, 1));
            }
            catch
            {
                return false;
            }
        }

        // Save secret and mark TOTP as enabled
        public async Task<bool> EnableTotpAsync(int userId, string secret)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return false;

            user.TotpSecret  = secret;
            user.TotpEnabled = true;
            await _context.SaveChangesAsync();
            return true;
        }

        // Disable TOTP for a user
        public async Task<bool> DisableTotpAsync(int userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return false;

            user.TotpSecret  = null;
            user.TotpEnabled = false;
            await _context.SaveChangesAsync();
            return true;
        }

        // Write a "trust this device" cookie valid for 30 days
        public void SetTrustedDeviceCookie(HttpResponse response, int userId)
        {
            var token = Convert.ToBase64String(
                System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));
            var value = $"{userId}:{token}";
            response.Cookies.Append(TrustedDeviceCookie, value, new CookieOptions
            {
                HttpOnly  = true,
                Secure    = true,
                SameSite  = SameSiteMode.Strict,
                Expires   = DateTimeOffset.UtcNow.AddDays(TrustDays)
            });
        }

        // Return true when the request carries a valid trusted-device cookie for this user
        public bool IsDeviceTrusted(HttpRequest request, int userId)
        {
            var cookie = request.Cookies[TrustedDeviceCookie];
            if (string.IsNullOrEmpty(cookie)) return false;
            var parts = cookie.Split(':');
            return parts.Length == 2 && parts[0] == userId.ToString();
        }
    }
}
