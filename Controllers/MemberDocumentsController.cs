using Microsoft.AspNetCore.Mvc;
using AdminMembers.Services;
using AdminMembers.Attributes;
using AdminMembers.Models;

namespace AdminMembers.Controllers
{
    [ApiController]
    [Route("api/members/{memberId:int}/documents")]
    public class MemberDocumentsController : ControllerBase
    {
        private readonly MemberDocumentService _documentService;

        public MemberDocumentsController(MemberDocumentService documentService)
        {
            _documentService = documentService;
        }

        private (int userId, string username) GetCaller()
        {
            var idHeader = Request.Headers["X-User-Id"].FirstOrDefault();
            var name     = Request.Headers["X-Username"].FirstOrDefault() ?? "unknown";
            int.TryParse(idHeader, out var id);
            return (id, name);
        }

        // GET /api/members/{memberId}/documents
        [HttpGet]
        [RequirePermission(Permission.Read)]
        public async Task<IActionResult> List(int memberId)
        {
            var docs = await _documentService.GetDocumentsAsync(memberId);
            return Ok(docs.Select(d => new
            {
                d.Id,
                d.FileName,
                d.ContentType,
                d.FileSizeBytes,
                d.UploadedByUsername,
                d.UploadedAt
            }));
        }

        // POST /api/members/{memberId}/documents
        [HttpPost]
        [RequirePermission(Permission.ReadWrite)]
        [RequestSizeLimit(22_000_000)]
        public async Task<IActionResult> Upload(int memberId, IFormFile file)
        {
            var (userId, username) = GetCaller();
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

            var (success, message, doc) = await _documentService.UploadAsync(memberId, file, userId, username, ip);

            if (!success)
                return BadRequest(new { error = message });

            return Ok(new
            {
                doc!.Id,
                doc.FileName,
                doc.ContentType,
                doc.FileSizeBytes,
                doc.UploadedByUsername,
                doc.UploadedAt,
                message
            });
        }

        // GET /api/members/{memberId}/documents/{documentId}/download
        [HttpGet("{documentId:int}/download")]
        [RequirePermission(Permission.Read)]
        public async Task<IActionResult> Download(int memberId, int documentId)
        {
            var (success, stream, fileName, contentType) = await _documentService.DownloadAsync(documentId);

            if (!success || stream == null)
                return NotFound(new { error = "Document not found." });

            return File(stream, contentType, fileName);
        }

        // DELETE /api/members/{memberId}/documents/{documentId}
        [HttpDelete("{documentId:int}")]
        [RequirePermission(Permission.ReadWrite)]
        public async Task<IActionResult> Delete(int memberId, int documentId)
        {
            var (userId, username) = GetCaller();
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

            var (success, message) = await _documentService.DeleteAsync(documentId, userId, username, ip);

            if (!success)
                return BadRequest(new { error = message });

            return Ok(new { message });
        }
    }
}
