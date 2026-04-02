using AdminMembers.Data;
using AdminMembers.Models;
using Microsoft.EntityFrameworkCore;

namespace AdminMembers.Services
{
    public class MemberDocumentService
    {
        private readonly ApplicationDbContext _context;
        private readonly BlobStorageService _blob;
        private readonly AuditLogService _audit;
        private readonly IConfiguration _configuration;
        private readonly ILogger<MemberDocumentService> _logger;

        private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            "application/pdf",
            "image/png", "image/jpeg", "image/gif", "image/webp",
            "application/msword",
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            "application/vnd.ms-excel",
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "text/plain"
        };

        private const long MaxFileSizeBytes = 20 * 1024 * 1024; // 20 MB

        public MemberDocumentService(
            ApplicationDbContext context,
            BlobStorageService blob,
            AuditLogService audit,
            IConfiguration configuration,
            ILogger<MemberDocumentService> logger)
        {
            _context = context;
            _blob = blob;
            _audit = audit;
            _configuration = configuration;
            _logger = logger;
        }

        private string Container =>
            _configuration["AzureStorageBlob:DocumentsContainerName"] ?? "documents";

        public async Task<List<MemberDocument>> GetDocumentsAsync(int memberId)
            => await _context.MemberDocuments
                .Where(d => d.MemberId == memberId)
                .OrderByDescending(d => d.UploadedAt)
                .AsNoTracking()
                .ToListAsync();

        public async Task<(bool Success, string Message, MemberDocument? Document)> UploadAsync(
            int memberId,
            IFormFile file,
            int uploadedByUserId,
            string uploadedByUsername,
            string ipAddress)
        {
            if (file == null || file.Length == 0)
                return (false, "No file provided.", null);

            if (file.Length > MaxFileSizeBytes)
                return (false, $"File exceeds the 20 MB limit.", null);

            var contentType = file.ContentType;
            if (!AllowedContentTypes.Contains(contentType))
                return (false, $"File type '{contentType}' is not allowed.", null);

            // Unique blob name: documents/member-{id}/{guid}-{originalname}
            var safeFileName = Path.GetFileName(file.FileName);
            var blobName = $"member-{memberId}/{Guid.NewGuid():N}-{safeFileName}";

            try
            {
                await using var stream = file.OpenReadStream();
                await _blob.UploadBlobAsync(Container, blobName, stream, contentType);

                var doc = new MemberDocument
                {
                    MemberId              = memberId,
                    FileName              = safeFileName,
                    BlobName              = blobName,
                    ContentType           = contentType,
                    FileSizeBytes         = file.Length,
                    UploadedByUserId      = uploadedByUserId,
                    UploadedByUsername    = uploadedByUsername,
                    UploadedAt            = DateTime.UtcNow
                };

                _context.MemberDocuments.Add(doc);
                await _context.SaveChangesAsync();

                await _audit.LogActionAsync(uploadedByUserId, uploadedByUsername,
                    "Document Uploaded", "Member", memberId,
                    $"Uploaded '{safeFileName}' for member {memberId}", ipAddress);

                return (true, "Document uploaded.", doc);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading document for member {MemberId}", memberId);
                return (false, "Upload failed. Please try again.", null);
            }
        }

        public async Task<(bool Success, Stream? Stream, string FileName, string ContentType)> DownloadAsync(int documentId)
        {
            var doc = await _context.MemberDocuments.FindAsync(documentId);
            if (doc == null)
                return (false, null, string.Empty, string.Empty);

            try
            {
                var ms = new MemoryStream();
                await _blob.DownloadBlobToStreamAsync(Container, doc.BlobName, ms);
                ms.Position = 0;
                return (true, ms, doc.FileName, doc.ContentType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading document {DocumentId}", documentId);
                return (false, null, string.Empty, string.Empty);
            }
        }

        public async Task<(bool Success, string Message)> DeleteAsync(
            int documentId,
            int deletedByUserId,
            string deletedByUsername,
            string ipAddress)
        {
            var doc = await _context.MemberDocuments.FindAsync(documentId);
            if (doc == null)
                return (false, "Document not found.");

            try
            {
                await _blob.DeleteBlobAsync(Container, doc.BlobName);
                _context.MemberDocuments.Remove(doc);
                await _context.SaveChangesAsync();

                await _audit.LogActionAsync(deletedByUserId, deletedByUsername,
                    "Document Deleted", "Member", doc.MemberId,
                    $"Deleted '{doc.FileName}' from member {doc.MemberId}", ipAddress);

                return (true, "Document deleted.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting document {DocumentId}", documentId);
                return (false, "Delete failed. Please try again.");
            }
        }
    }
}
