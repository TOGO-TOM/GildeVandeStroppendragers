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
        private readonly BlobStorageService? _blobStorageService;
        private readonly IConfiguration _configuration;

        public BackupService(ApplicationDbContext context, ILogger<BackupService> logger, IConfiguration configuration, BlobStorageService? blobStorageService = null)
        {
            _context = context;
            _logger = logger;
            _configuration = configuration;
            _blobStorageService = blobStorageService;
        }

        private string GetBackupPassword(string? overridePassword = null)
            => overridePassword
               ?? _configuration["Backup:Password"]
               ?? throw new InvalidOperationException("Backup:Password is not configured. Set it in Azure App Service environment variables.");

        public async Task<byte[]> CreateEncryptedBackup(string? password = null)
        {
            try
            {
                var members = await _context.Members
                    .Include(m => m.Address)
                    .AsNoTracking()
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
                var encryptedData = EncryptData(jsonData, GetBackupPassword(password));

                _logger.LogInformation("Backup created successfully with {MemberCount} members", members.Count);

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
                var jsonData = DecryptData(encryptedData, GetBackupPassword(password));

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

            // Generate key and IV from password using static Pbkdf2 method
            var salt = Encoding.UTF8.GetBytes("AdminMembersSalt2024");
            aes.Key = Rfc2898DeriveBytes.Pbkdf2(password, salt, 10000, HashAlgorithmName.SHA256, 32);
            aes.IV = Rfc2898DeriveBytes.Pbkdf2(password, salt, 10000, HashAlgorithmName.SHA256, 16);

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

            // Generate key from password using static Pbkdf2 method
            var salt = Encoding.UTF8.GetBytes("AdminMembersSalt2024");
            aes.Key = Rfc2898DeriveBytes.Pbkdf2(password, salt, 10000, HashAlgorithmName.SHA256, 32);

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

        /// <summary>
        /// Uploads a backup to Azure Blob Storage
        /// </summary>
        public async Task<string> UploadBackupToBlobAsync(byte[] encryptedBackup, string? fileName = null)
        {
            if (_blobStorageService == null)
            {
                throw new InvalidOperationException("Azure Blob Storage is not configured. Cannot upload backup.");
            }

            try
            {
                var containerName = _configuration.GetValue<string>("AzureStorageBlob:BackupContainerName") ?? "backups";
                var blobName = fileName ?? $"backup_{DateTime.UtcNow:yyyyMMdd_HHmmss}.bak";

                var blobUri = await _blobStorageService.UploadBlobAsync(containerName, blobName, encryptedBackup, "application/octet-stream");

                _logger.LogInformation($"Backup uploaded to Azure Blob Storage: {blobName}");

                return blobUri;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading backup to Azure Blob Storage");
                throw;
            }
        }

        /// <summary>
        /// Lists all available backups from Azure Blob Storage
        /// </summary>
        public async Task<List<BlobInfo>> ListBackupsFromBlobAsync()
        {
            if (_blobStorageService == null)
            {
                throw new InvalidOperationException("Azure Blob Storage is not configured. Cannot list backups.");
            }

            try
            {
                var containerName = _configuration.GetValue<string>("AzureStorageBlob:BackupContainerName") ?? "backups";
                var blobs = await _blobStorageService.ListBlobsAsync(containerName);

                _logger.LogInformation($"Listed {blobs.Count} backups from Azure Blob Storage");

                return blobs;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error listing backups from Azure Blob Storage");
                throw;
            }
        }

        /// <summary>
        /// Downloads a backup from Azure Blob Storage
        /// </summary>
        public async Task<byte[]> DownloadBackupFromBlobAsync(string blobName)
        {
            if (_blobStorageService == null)
            {
                throw new InvalidOperationException("Azure Blob Storage is not configured. Cannot download backup.");
            }

            try
            {
                var containerName = _configuration.GetValue<string>("AzureStorageBlob:BackupContainerName") ?? "backups";
                var backupData = await _blobStorageService.DownloadBlobAsync(containerName, blobName);

                _logger.LogInformation($"Downloaded backup {blobName} from Azure Blob Storage");

                return backupData;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error downloading backup {blobName} from Azure Blob Storage");
                throw;
            }
        }

        /// <summary>
        /// Deletes a backup from Azure Blob Storage
        /// </summary>
        public async Task<bool> DeleteBackupFromBlobAsync(string blobName)
        {
            if (_blobStorageService == null)
            {
                throw new InvalidOperationException("Azure Blob Storage is not configured. Cannot delete backup.");
            }

            try
            {
                var containerName = _configuration.GetValue<string>("AzureStorageBlob:BackupContainerName") ?? "backups";
                var deleted = await _blobStorageService.DeleteBlobAsync(containerName, blobName);

                if (deleted)
                {
                    _logger.LogInformation($"Deleted backup {blobName} from Azure Blob Storage");
                }

                return deleted;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting backup {blobName} from Azure Blob Storage");
                throw;
            }
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
