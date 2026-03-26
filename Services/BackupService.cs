using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AdminMembers.Data;
using AdminMembers.Models;
using Microsoft.EntityFrameworkCore;

namespace AdminMembers.Services
{
    public class BackupService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<BackupService> _logger;
        private const string DefaultPassword = "AdminMembers2024!SecureBackup"; // Change this in production!

        public BackupService(ApplicationDbContext context, ILogger<BackupService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<byte[]> CreateEncryptedBackup(string? password = null)
        {
            try
            {
                // Get all members with addresses
                var members = await _context.Members
                    .Include(m => m.Address)
                    .ToListAsync();

                var backup = new BackupData
                {
                    BackupDate = DateTime.UtcNow,
                    Version = "1.0",
                    Members = members
                };

                // Serialize to JSON
                var jsonData = JsonSerializer.Serialize(backup, new JsonSerializerOptions
                {
                    WriteIndented = true
                });

                // Encrypt the data
                var encryptedData = EncryptData(jsonData, password ?? DefaultPassword);

                _logger.LogInformation($"Backup created successfully with {members.Count} members");

                return encryptedData;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating backup");
                throw;
            }
        }

        public async Task<RestoreResult> RestoreFromEncryptedBackup(byte[] encryptedData, string? password = null, bool overwrite = false)
        {
            try
            {
                // Decrypt the data
                var jsonData = DecryptData(encryptedData, password ?? DefaultPassword);

                // Deserialize from JSON
                var backup = JsonSerializer.Deserialize<BackupData>(jsonData);

                if (backup == null || backup.Members == null)
                {
                    throw new InvalidOperationException("Invalid backup file format");
                }

                var result = new RestoreResult
                {
                    BackupDate = backup.BackupDate,
                    TotalMembers = backup.Members.Count
                };

                if (overwrite)
                {
                    // Clear existing data
                    var existingMembers = await _context.Members.ToListAsync();
                    _context.Members.RemoveRange(existingMembers);
                    await _context.SaveChangesAsync();

                    // Reset identity seed (optional)
                    await _context.Database.ExecuteSqlRawAsync("DBCC CHECKIDENT ('Members', RESEED, 0)");
                    await _context.Database.ExecuteSqlRawAsync("DBCC CHECKIDENT ('Addresses', RESEED, 0)");
                }

                // Import members
                foreach (var member in backup.Members)
                {
                    // Check if member number already exists
                    var exists = await _context.Members.AnyAsync(m => m.MemberNumber == member.MemberNumber);
                    
                    if (!exists || overwrite)
                    {
                        // Reset IDs for new insertion
                        member.Id = 0;
                        if (member.Address != null)
                        {
                            member.Address.Id = 0;
                            member.Address.MemberId = 0;
                        }

                        _context.Members.Add(member);
                        result.ImportedMembers++;
                    }
                    else
                    {
                        result.SkippedMembers++;
                    }
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation($"Restore completed: {result.ImportedMembers} imported, {result.SkippedMembers} skipped");

                return result;
            }
            catch (CryptographicException)
            {
                throw new InvalidOperationException("Invalid password or corrupted backup file");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error restoring backup");
                throw;
            }
        }

        private byte[] EncryptData(string plainText, string password)
        {
            using var aes = Aes.Create();
            
            // Generate key from password
            using var keyDerivation = new Rfc2898DeriveBytes(password, 
                Encoding.UTF8.GetBytes("AdminMembersSalt2024"), // Salt
                10000, // Iterations
                HashAlgorithmName.SHA256);
            
            aes.Key = keyDerivation.GetBytes(32); // 256-bit key
            aes.IV = keyDerivation.GetBytes(16);  // 128-bit IV

            using var encryptor = aes.CreateEncryptor();
            using var msEncrypt = new MemoryStream();
            
            // Write IV to the beginning of the stream
            msEncrypt.Write(aes.IV, 0, aes.IV.Length);

            using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
            using (var swEncrypt = new StreamWriter(csEncrypt))
            {
                swEncrypt.Write(plainText);
            }

            return msEncrypt.ToArray();
        }

        private string DecryptData(byte[] cipherText, string password)
        {
            using var aes = Aes.Create();
            
            // Generate key from password
            using var keyDerivation = new Rfc2898DeriveBytes(password, 
                Encoding.UTF8.GetBytes("AdminMembersSalt2024"), // Same salt as encryption
                10000,
                HashAlgorithmName.SHA256);
            
            aes.Key = keyDerivation.GetBytes(32);

            // Extract IV from the beginning of the data
            var iv = new byte[16];
            Array.Copy(cipherText, 0, iv, 0, iv.Length);
            aes.IV = iv;

            using var decryptor = aes.CreateDecryptor();
            using var msDecrypt = new MemoryStream(cipherText, iv.Length, cipherText.Length - iv.Length);
            using var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read);
            using var srDecrypt = new StreamReader(csDecrypt);
            
            return srDecrypt.ReadToEnd();
        }
    }

    public class BackupData
    {
        public DateTime BackupDate { get; set; }
        public string Version { get; set; } = "1.0";
        public List<Member> Members { get; set; } = new();
    }

    public class RestoreResult
    {
        public DateTime BackupDate { get; set; }
        public int TotalMembers { get; set; }
        public int ImportedMembers { get; set; }
        public int SkippedMembers { get; set; }
    }
}
